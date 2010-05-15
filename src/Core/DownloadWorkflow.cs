using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;
using NPackage.Core.Extensions;

namespace NPackage.Core
{
    public class DownloadWorkflow
    {
        private class DownloadAction
        {
            private readonly Uri uri;
            private readonly string filename;
            private readonly Action<string> continuation;

            public DownloadAction(Uri uri, string filename, Action<string> continuation)
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

            public Action<string> Continuation
            {
                get { return continuation; }
            }
        }

        private readonly List<DownloadAction> actions = new List<DownloadAction>();

        public void Enqueue(Uri uri, string filename, Action<string> continuation)
        {
            actions.Add(new DownloadAction(uri, filename, continuation));
        }

        private static string AddFilename(string pathOrFilename, string filename)
        {
            bool isPath = pathOrFilename.EndsWith(Path.DirectorySeparatorChar.ToString())
                || pathOrFilename.EndsWith(Path.AltDirectorySeparatorChar.ToString());

            return isPath
                ? Path.Combine(pathOrFilename, filename)
                : pathOrFilename;
        }

        public bool Step()
        {
            var actionsByFilenameByUri = actions
                .GroupBy(a => a.Uri)
                .Select(g => new
                                 {
                                     Uri = g.Key,
                                     ActionsByFilename = g
                                 .GroupBy(a => a.Filename, StringComparer.InvariantCultureIgnoreCase)
                                 .Select(g2 => new { Filename = g2.Key, Actions = g2.ToArray() })
                                 .OrderBy(a => a.Filename, StringComparer.InvariantCultureIgnoreCase)
                                 .ToArray()
                                 })
                .OrderBy(a => a.Uri.ToString())
                .ToArray();

            actions.Clear();

            foreach (var uriPair in actionsByFilenameByUri)
            {
                string firstFilename = null;
                FileInfo firstFileInfo = null;
                string responseUriFilename = null;

                foreach (var filenamePair in uriPair.ActionsByFilename)
                {
                    if (firstFilename == null)
                    {
                        string responseFilename;
                        RaiseLog("Checking {0}", uriPair.Uri);

                        WebRequest request = WebRequest.Create(uriPair.Uri);
                        using (WebResponse response = request.GetResponse())
                        {
                            responseUriFilename = Path.GetFileName(response.ResponseUri.GetComponents(UriComponents.Path, UriFormat.Unescaped));
                          
                            string contentDisposition = response.Headers["Content-Disposition"];
                            if (!String.IsNullOrEmpty(contentDisposition))
                            {
                                string[] parts = contentDisposition.Split(new[] { ';' }, 2);
                                if (parts.Length > 1)
                                {
                                    string part1 = parts[1].TrimStart();
                                    const string prefix = "filename=";
                                    if (part1.StartsWith(prefix))
                                        responseUriFilename = part1.Substring(prefix.Length).Trim('"').Trim();
                                }
                            }

                            responseFilename = AddFilename(filenamePair.Filename, responseUriFilename);
                            FileInfo fileInfo = new FileInfo(responseFilename);

                            HttpWebResponse httpWebResponse = response as HttpWebResponse;
                            if (!fileInfo.Exists || (httpWebResponse != null && httpWebResponse.LastModified > fileInfo.LastWriteTime))
                            {
                                using (Stream inputStream = response.GetResponseStream())
                                {
                                    RaiseLog("Downloading from {0} to {1}", response.ResponseUri, responseFilename);
                                    inputStream.CopyTo(responseFilename);
                                    fileInfo.Refresh();

                                    if (httpWebResponse != null)
                                        fileInfo.LastWriteTime = httpWebResponse.LastModified;
                                }
                            }
                            
                            firstFilename = responseFilename;
                            firstFileInfo = fileInfo;
                        }
                        
                        foreach (DownloadAction action in filenamePair.Actions)
                            action.Continuation(responseFilename);
                    }
                    else
                    {
                        string responseFilename = AddFilename(filenamePair.Filename, responseUriFilename);
                        FileInfo fileInfo = new FileInfo(responseFilename);

                        if (!fileInfo.Exists || firstFileInfo.LastWriteTime > fileInfo.LastWriteTime)
                        {
                            RaiseLog("Copying from {0} to {1}", firstFilename, responseFilename);
                            File.Copy(firstFilename, responseFilename, true);
                            fileInfo.LastWriteTime = firstFileInfo.LastWriteTime;
                        }

                        foreach (DownloadAction action in filenamePair.Actions)
                            action.Continuation(responseFilename);
                    }
                }
            }
            
            return actions.Count > 0;
        }

        private void RaiseLog(string format, params object[] args)
        {
            EventHandler<LogEventArgs> handler = Log;
            if (handler != null)
                handler(this, new LogEventArgs(String.Format(format, args)));
        }

        private static InvalidOperationException NotFoundInArchive(string archiveFilename, string entryName)
        {
            string message = String.Format("There is no {0} in {1}.", entryName, archiveFilename);
            throw new InvalidOperationException(message);
        }

        public static void ExtractFile(string archiveFilename, string entryName, string filename)
        {
            if (archiveFilename.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
            {
                using (ZipFile file = new ZipFile(archiveFilename))
                {
                    int index = file.FindEntry(entryName, true);
                    if (index < 0)
                        throw NotFoundInArchive(archiveFilename, entryName);

                    using (Stream stream = file.GetInputStream(index))
                        stream.CopyTo(filename);
                }
            }
            else if (archiveFilename.EndsWith(".tar.gz", StringComparison.InvariantCultureIgnoreCase) ||
                     archiveFilename.EndsWith(".tgz", StringComparison.InvariantCultureIgnoreCase))
            {
                using (Stream fileStream = File.Open(archiveFilename, FileMode.Open, FileAccess.Read))
                using (Stream gzipStream = new GZipInputStream(fileStream))
                using (TarInputStream tarStream = new TarInputStream(gzipStream))
                {
                    TarEntry entry;
                    while ((entry = tarStream.GetNextEntry()) != null)
                    {
                        if (String.Equals(entry.Name, entryName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            using (FileStream outputStream = File.Create(filename))
                                tarStream.CopyEntryContents(outputStream);

                            break;
                        }
                    }

                    if (entry == null)
                        throw NotFoundInArchive(archiveFilename, entryName);
                }
            }
            else
                throw new NotSupportedException(archiveFilename + " is not a recognised archive.");
        }

        public event EventHandler<LogEventArgs> Log;
    }
}