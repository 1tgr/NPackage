using System;
using System.Collections.Generic;
using System.IO;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using NPackage.Core.Extensions;
using Mono.Options;

namespace NPackage.Core
{
    public class InstallCommand : CommandBase
    {
        private static readonly JsonSerializer serializer = new JsonSerializer();
        private readonly DownloadWorkflow workflow = new DownloadWorkflow();
        private readonly string libPath = FindLibDirectory();
        private readonly string archivePath;
        private bool logNeedsIndent;

        public InstallCommand()
        {
            RepositoryUri = new Uri("http://np.partario.com/packages.js");

            archivePath = Path.Combine(libPath, ".dist");
            workflow.Log += OnWorkflowLog;
        }

        protected override void AddOptions(OptionSet set)
        {
            base.AddOptions(set);
            set.Add("r|repository", "URL of the packages.js file", (Uri v) => RepositoryUri = v);
        }

        private void Log(string format, params object[] args)
        {
            string message = string.Format(format, args);

            Console.WriteLine(logNeedsIndent
                ? "        " + message
                : message);

            logNeedsIndent = true;
        }

        private void OnWorkflowLog(object sender, LogEventArgs e)
        {
            Log(e.Message);
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

        private static Package FindPackage(Repository repository, string name)
        {
            foreach (Package package in repository.Packages)
            {
                if (package.Name == name ||
                    package.Name + "-" + package.Version == name)
                {
                    return package;
                }
            }

            string message = string.Format("There is no package called {0}.", name);
            throw new ArgumentException(message, "name");
        }

        private void InstallPackages(string repositoryFilename)
        {
            Repository repository;

            using (TextReader textReader = new StreamReader(repositoryFilename))
            using (JsonReader jsonReader = new JsonTextReader(textReader))
                repository = serializer.Deserialize<Repository>(jsonReader);

            foreach (string name in PackageNames)
            {
                Package package = FindPackage(repository, name);
                DownloadPackageContents(package);
            }
        }

        private void DownloadPackageContents(Package package)
        {
            Uri siteUri = package.MasterSites.Count > 0
                ? new Uri(RepositoryUri, package.MasterSites[0])
                : RepositoryUri;

            string packagePath = Path.Combine(libPath, Path.Combine(package.Name, package.Version));
            Directory.CreateDirectory(packagePath);

            foreach (KeyValuePair<string, Library> pair in package.Libraries)
            {
                Uri downloadUri = new Uri(siteUri, pair.Value.Binary);
                string filename = Path.Combine(packagePath, pair.Key);
                if (string.IsNullOrEmpty(downloadUri.Fragment))
                    workflow.Enqueue(downloadUri, filename, delegate { LogSuccess(downloadUri, filename); });
                else
                {
                    UriBuilder archiveUriBuilder = new UriBuilder(downloadUri) { Fragment = String.Empty };
                    Uri archiveUri = archiveUriBuilder.Uri;
                    workflow.Enqueue(archiveUri, archivePath + Path.DirectorySeparatorChar, archiveFilename => UnpackArchive(archiveFilename, downloadUri, filename));
                }
            }
        }

        private static void LogSuccess(Uri uri, string filename)
        {
            Console.WriteLine("\r ***    Installed {0} to {1}", uri, filename);
        }

        private void UnpackArchive(string archiveFilename, Uri uri, string filename)
        {
            FileInfo archiveFileInfo = new FileInfo(archiveFilename);
            FileInfo fileInfo = new FileInfo(filename);

            if (!fileInfo.Exists || archiveFileInfo.LastWriteTime > fileInfo.LastWriteTime)
            {
                Log("Unpacking {0} to {1}", archiveFilename, filename);

                string entryName = uri.Fragment.TrimStart('#');
                ExtractFile(archiveFilename, entryName, filename);

                LogSuccess(uri, filename);
            }
        }

        private static InvalidOperationException NotFoundInArchive(string archiveFilename, string entryName)
        {
            string message = String.Format("There is no {0} in {1}.", entryName, archiveFilename);
            throw new InvalidOperationException(message);
        }

        private void ExtractFile(string archiveFilename, string entryName, string filename)
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
                        if (string.Equals(entry.Name, entryName, StringComparison.InvariantCultureIgnoreCase))
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

        protected override int RunCore()
        {
            Directory.CreateDirectory(archivePath);
            workflow.Enqueue(RepositoryUri, archivePath + Path.DirectorySeparatorChar, InstallPackages);

            int steps = 1;
            do
            {
                Console.Write("[ {0,3} ] ", steps);
                logNeedsIndent = false;
                steps++;
            } while (workflow.Step());

            return 0;
        }

        public Uri RepositoryUri { get; set; }

        public IList<string> PackageNames
        {
            get { return ExtraArguments; }
        }
    }
}