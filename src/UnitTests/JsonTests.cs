using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using NPackage.Core;
using NUnit.Framework;

namespace NPackage.UnitTests
{
    [TestFixture]
    public class JsonTests
    {
        private static void Convert(string filename)
        {
            Package package = new Package();
            using (TextReader reader = new StreamReader(filename))
                PackageParser.ParseYaml(reader, package);

            string path = Path.Combine(@"c:\git\NPackage\web-json", package.Name + "-" + package.Version);
            Directory.CreateDirectory(path);

            using (TextWriter stringWriter = new StreamWriter(Path.Combine(path, package.Name + ".np")))
            using (JsonWriter jsonWriter = new JsonTextWriter(stringWriter) { Formatting = Formatting.Indented })
                new JsonSerializer().Serialize(jsonWriter, package);
        }

        [Test]
        public void Serialize()
        {
            foreach (DirectoryInfo directoryInfo in new DirectoryInfo(@"c:\git\NPackage\web").GetDirectories())
            {
                foreach (FileInfo fileInfo in directoryInfo.GetFiles("*.np"))
                {
                    Convert(fileInfo.FullName);
                    Console.WriteLine("Converted " + fileInfo.FullName);
                }
            }
        }
    }
}