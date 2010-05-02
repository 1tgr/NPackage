using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using ICSharpCode.SharpZipLib.Zip;
using NPackage.Core;

namespace NPackage.Console
{
    internal static class Program
    {
        private class NestedValue
        {
            private readonly int nesting;
            private readonly object value;

            public NestedValue(int nesting, object value)
            {
                this.nesting = nesting;
                this.value = value;
            }

            public int Nesting
            {
                get { return nesting; }
            }

            public object Value
            {
                get { return value; }
            }
        }

        private class DownloadAction
        {
            private readonly Uri uri;
            private readonly string filename;
            private readonly Action continuation;

            public DownloadAction(Uri uri, string filename, Action continuation)
            {
                this.uri = uri;
                this.filename = filename;
                this.continuation = continuation;
            }

            public Uri Uri
            {
                get { return uri; }
            }

            public string Filename
            {
                get { return filename; }
            }

            public Action Continuation
            {
                get { return continuation; }
            }
        }

        private static void Main(string[] args)
        {
            foreach (string arg in args)
            {
                try
                {
                    Download(new Uri(arg));
                }
                catch (Exception ex)
                {
                    System.Console.Error.WriteLine("Unable to download {0}. {1}", arg, ex.Message);
                }
            }
        }

        private static void Download(Uri packageUri)
        {
            Package package = new Package();

            {
                WebRequest request = WebRequest.Create(packageUri);
                using (WebResponse response = request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (TextReader reader = new StreamReader(stream))
                    ParseYaml(reader, package);
            }

            if (package.MasterSites == null)
                package.MasterSites = packageUri.GetLeftPart(UriPartial.Path);

            List<string> parts = new List<string>(Environment.CurrentDirectory.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            parts.Add("lib");

            string path = null;
            while (parts.Count >= 2 && !Directory.Exists(path = string.Join(Path.DirectorySeparatorChar.ToString(), parts.ToArray())))
                parts.RemoveAt(parts.Count - 2);

            if (path == null || parts.Count == 1)
                throw new InvalidOperationException("Couldn't find lib directory.");

            string archivePath = Path.Combine(path, ".dist");
            string packagePath = Path.Combine(path, Path.Combine(package.Name, package.Version));
            Directory.CreateDirectory(packagePath);

            List<DownloadAction> actions = new List<DownloadAction>();
            foreach (KeyValuePair<string, Library> pair in package.Library)
            {
                Uri downloadUri = new Uri(new Uri(package.MasterSites), pair.Value.Binary);
                string filename = Path.Combine(packagePath, pair.Key);
                if (string.IsNullOrEmpty(downloadUri.Fragment))
                    actions.Add(new DownloadAction(downloadUri, filename, () => ExtractFile(downloadUri, filename)));
                else
                {
                    UriBuilder builder = new UriBuilder(downloadUri) { Fragment = string.Empty };
                    Uri archiveUri = builder.Uri;
                    string archiveFilename = Path.Combine(archivePath, Path.GetFileName(archiveUri.GetComponents(UriComponents.Path, UriFormat.Unescaped)));
                    Directory.CreateDirectory(archivePath);
                    actions.Add(new DownloadAction(builder.Uri, archiveFilename, () => UnpackArchive(archiveFilename, downloadUri, filename)));
                }
            }

            while (actions.Count > 0)
            {
                var actionsByFilenameByUri = actions
                    .GroupBy(a => a.Uri)
                    .Select(g => new
                                     {
                                         Uri = g.Key,
                                         ActionsByFilename = g
                                     .GroupBy(a => a.Filename, StringComparer.InvariantCultureIgnoreCase)
                                     .Select(g2 => new { Filename = g2.Key, Actions = g2.ToArray() })
                                     .ToArray()
                                     })
                    .ToArray();

                actions.Clear();

                foreach (var uriPair in actionsByFilenameByUri)
                {
                    string firstFilename = null;

                    foreach (var filenamePair in uriPair.ActionsByFilename)
                    {
                        if (firstFilename == null)
                        {
                            firstFilename = filenamePair.Filename;
                            WebRequest request = WebRequest.Create(uriPair.Uri);
                            using (WebResponse response = request.GetResponse())
                            using (Stream inputStream = response.GetResponseStream())
                            {
                                System.Console.WriteLine("Downloading from {0} to {1}", uriPair.Uri, filenamePair.Filename);
                                CopyStream(inputStream, firstFilename);
                            }
                        }
                        else
                        {
                            System.Console.WriteLine("Copying from {0} to {1}", firstFilename, filenamePair.Filename);
                            File.Copy(firstFilename, filenamePair.Filename, true);
                        }

                        foreach (DownloadAction action in filenamePair.Actions)
                            action.Continuation();
                    }
                }
            }
        }

        private static void ExtractFile(Uri uri, string filename)
        {
            System.Console.WriteLine("Installed {0} to {1}", uri, filename);
        }

        private static void CopyStream(Stream inputStream, string outputFilename)
        {
            using (Stream outputStream = File.Create(outputFilename))
            {
                int count;
                byte[] chunk = new byte[4096];
                while ((count = inputStream.Read(chunk, 0, chunk.Length)) > 0)
                    outputStream.Write(chunk, 0, count);
            }
        }

        private static void UnpackArchive(string archiveFilename, Uri uri, string filename)
        {
            System.Console.WriteLine("Unpacking {0} to {1}", archiveFilename, filename);

            string tempPath = Path.Combine(Path.GetDirectoryName(archiveFilename), Path.GetRandomFileName());
            Directory.CreateDirectory(tempPath);

            string fileFilter = uri.Fragment
                .TrimStart('#')
                .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            new FastZip().ExtractZip(archiveFilename, tempPath, fileFilter);
            string extractedFilename = Path.Combine(tempPath, Path.Combine(Path.GetFileNameWithoutExtension(archiveFilename), fileFilter));
            File.Delete(filename);
            File.Move(extractedFilename, filename);
            ExtractFile(uri, filename);
            Directory.Delete(tempPath, true);
        }

        private static void ParseYaml(TextReader reader, object obj)
        {
            Stack<NestedValue> stack = new Stack<NestedValue>();
            stack.Push(new NestedValue(0, obj));

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                int nesting = 0;
                while (line.Length > nesting && char.IsWhiteSpace(line, nesting))
                    nesting++;

                string s = line.Trim();
                if (s.Length == 0)
                    continue;

                string[] parts = s.Split(new[] { ':' }, 2);
                if (parts.Length < 2)
                    continue;

                NestedValue value = stack.Peek();
                while (nesting < value.Nesting)
                    value = stack.Pop();

                PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(value.Value);
                string name = parts[0].Replace("-", string.Empty);
                PropertyDescriptor property = properties[name];
                if (property == null)
                    continue;

                Type propertyType = property.PropertyType;
                if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                {
                    Type[] genericArguments = propertyType.GetGenericArguments();
                    MethodInfo addMethod = propertyType.GetMethod("Add", genericArguments);
                    object dictionary = property.GetValue(value.Value);
                    object innerValue = Activator.CreateInstance(genericArguments[1]);
                    addMethod.Invoke(dictionary, new[] { parts[1].TrimStart(), innerValue });
                    stack.Push(new NestedValue(nesting + 1, innerValue));
                }
                else
                    property.SetValue(value.Value, parts[1].TrimStart());
            }
        }
    }
}
