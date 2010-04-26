using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Reflection;

namespace NPackage.Console
{
    public class Package
    {
        private readonly Dictionary<string, Library> library = new Dictionary<string, Library>();

        /*
            Name:	nunit
            Version:	2.5.5.10112
            Description:	Test framework for all .Net languages, running on Microsoft .NET and Mono
            Author:	Charlie Poole
            Maintainer:	tim.g.robinson@gmail.com
            Master-Sites:	http://launchpad.net/nunitv2/2.5/2.5.5/+download/

            Library: nunit.framework.dll
                Binary: NUnit-2.5.5.10112.zip#bin/net-2.0/nunit.framework.dll
         */

        public string Name { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        public string Maintainer { get; set; }
        public string MasterSites { get; set; }

        public IDictionary<string, Library> Library
        {
            get { return library; }
        }
    }

    public class Library
    {
        public string Binary { get; set; }
    }

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

        private static void Main()
        {
            Package package = new Package();

            Uri packageUri = new Uri("http://veneto/~tim/np/nunit-2.5.5.10112/nunit.np");
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
            string path = null;
            while (parts.Count > 0 && !Directory.Exists(path = Path.Combine(string.Join(Path.DirectorySeparatorChar.ToString(), parts.ToArray()), "lib")))
                parts.RemoveAt(parts.Count - 1);

            if (path == null || parts.Count == 0)
                throw new InvalidOperationException("Couldn't find lib directory.");

            foreach (KeyValuePair<string, Library> pair in package.Library)
            {
                Uri libraryUri = new Uri(new Uri(package.MasterSites), pair.Value.Binary);
                string filename = Path.Combine(path, pair.Key);
                System.Console.WriteLine("Installing {0} to {1}", libraryUri, filename);

                WebRequest request = WebRequest.Create(libraryUri);
                using (WebResponse response = request.GetResponse())
                using (Stream inputStream = response.GetResponseStream())
                using (Stream outputStream = File.Create(filename))
                {
                    int count;
                    byte[] chunk = new byte[4096];
                    while ((count = inputStream.Read(chunk, 0, chunk.Length)) > 0)
                        outputStream.Write(chunk, 0, count);
                }
            }
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