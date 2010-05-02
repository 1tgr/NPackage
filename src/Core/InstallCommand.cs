using System;
using System.Collections.Generic;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using NPackage.Core.Extensions;

namespace NPackage.Core
{
    public class InstallCommand
    {
        private readonly DownloadWorkflow workflow = new DownloadWorkflow();
        private readonly string libPath = FindLibDirectory();
        private readonly string archivePath;

        public InstallCommand()
        {
            archivePath = Path.Combine(libPath, ".dist");
            workflow.Log += OnWorkflowLog;
        }

        private static void OnWorkflowLog(object sender, LogEventArgs e)
        {
            Console.WriteLine("\t" + e.Message);
        }

        public static string FindLibDirectory()
        {
            string path = "lib";
            string root = Path.Combine(Path.GetPathRoot(Environment.CurrentDirectory), "lib");
            while (!Directory.Exists(path))
            {
                string fullPath = Path.GetFullPath(path);
                if (string.Equals(root, fullPath, StringComparison.InvariantCultureIgnoreCase))
                    throw new InvalidOperationException("Couldn't find lib directory.");

                path = Path.Combine("..", path);
            }

            return path;
        }

        private void DownloadPackageFile(Uri packageUri)
        {
            string packageFilename = Path.Combine(archivePath, Path.GetFileName(packageUri.GetComponents(UriComponents.Path, UriFormat.Unescaped)));
            workflow.Enqueue(packageUri, packageFilename, () => DownloadPackageContents(packageUri, packageFilename));
        }

        private void DownloadPackageContents(Uri packageUri, string packageFilename)
        {
            Package package = new Package();

            using (TextReader reader = new StreamReader(packageFilename))
                PackageParser.ParseYaml(reader, package);

            Uri siteUri = new Uri(packageUri.GetLeftPart(UriPartial.Path));
            if (package.MasterSites != null)
                siteUri = new Uri(siteUri, package.MasterSites);

            string packagePath = Path.Combine(libPath, Path.Combine(package.Name, package.Version));
            Directory.CreateDirectory(packagePath);

            foreach (KeyValuePair<string, Library> pair in package.Library)
            {
                Uri downloadUri = new Uri(siteUri, pair.Value.Binary);
                string filename = Path.Combine(packagePath, pair.Key);
                if (string.IsNullOrEmpty(downloadUri.Fragment))
                    workflow.Enqueue(downloadUri, filename, () => ExtractFile(downloadUri, filename));
                else
                {
                    UriBuilder archiveUriBuilder = new UriBuilder(downloadUri) { Fragment = String.Empty };
                    Uri archiveUri = archiveUriBuilder.Uri;
                    string archiveFilename = Path.Combine(archivePath, Path.GetFileName(archiveUri.GetComponents(UriComponents.Path, UriFormat.Unescaped)));
                    workflow.Enqueue(archiveUri, archiveFilename, () => UnpackArchive(archiveFilename, downloadUri, filename));
                }
            }
        }

        private static void ExtractFile(Uri uri, string filename)
        {
            Console.WriteLine("Installed {0} to {1}", uri, filename);
        }

        private static void UnpackArchive(string archiveFilename, Uri uri, string filename)
        {
            FileInfo archiveFileInfo = new FileInfo(archiveFilename);
            FileInfo fileInfo = new FileInfo(filename);

            if (!fileInfo.Exists || archiveFileInfo.LastWriteTime > fileInfo.LastWriteTime)
            {
                Console.WriteLine("\tUnpacking {0} to {1}", archiveFilename, filename);

                using (ZipFile file = new ZipFile(archiveFilename))
                {
                    string entryName = uri.Fragment.TrimStart('#');

                    int index = file.FindEntry(entryName, true);
                    if (index < 0)
                    {
                        string message = String.Format("There is no {0} in {1}.", entryName, archiveFilename);
                        throw new InvalidOperationException(message);
                    }

                    using (Stream stream = file.GetInputStream(index))
                        stream.CopyTo(filename);
                }

                ExtractFile(uri, filename);
            }
        }

        public int Run(string[] args)
        {
            Directory.CreateDirectory(archivePath);

            foreach (string arg in args)
                DownloadPackageFile(new Uri(arg));

            workflow.Run();
            return 0;
        }
    }
}