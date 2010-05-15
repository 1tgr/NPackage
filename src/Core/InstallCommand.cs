using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Mono.Options;

namespace NPackage.Core
{
    public class InstallCommand : CommandBase
    {
        private static readonly JsonSerializer serializer = new JsonSerializer();
        private readonly DownloadWorkflow workflow = new DownloadWorkflow();
        private readonly string libPath = FindLibDirectory();
        private readonly string archivePath;
        private readonly string archiveDirectory;
        private readonly Dictionary<string, Package> packages = new Dictionary<string, Package>(StringComparer.InvariantCultureIgnoreCase);
        private bool logNeedsIndent;
        private int steps;

        public InstallCommand()
        {
            RepositoryUri = new Uri("http://np.partario.com/packages.js");

            workflow.Log += OnWorkflowLog;
            archivePath = Path.Combine(libPath, ".dist");
            archiveDirectory = archivePath + Path.DirectorySeparatorChar;
        }

        protected override OptionSet CreateOptionSet()
        {
            return base
                .CreateOptionSet()
                .Add("r|repository", "URL of the packages.js file", (Uri v) => RepositoryUri = v);
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

        private Package FindPackage(string name)
        {
            Package package;
            if (packages.TryGetValue(name, out package))
                return package;
            else
            {
                string message = string.Format("There is no package called {0}.", name);
                throw new ArgumentException(message, "name");
            }
        }

        private void RegisterPackage(Package package)
        {
            packages[package.Name + "-" + package.Version] = package;

            Package latestPackage;
            if (!packages.TryGetValue(package.Name, out latestPackage) ||
                string.Compare(package.Version, latestPackage.Version, StringComparison.InvariantCultureIgnoreCase) > 0)
            {
                packages[package.Name] = package;
            }
        }

        private void RegisterPackage(string filename)
        {
            Package package;

            using (TextReader textReader = new StreamReader(filename))
            using (JsonReader jsonReader = new JsonTextReader(textReader))
                package = serializer.Deserialize<Package>(jsonReader);

            RegisterPackage(package);
        }

        private void RegisterRepository(string filename)
        {
            Repository repository;

            using (TextReader textReader = new StreamReader(filename))
            using (JsonReader jsonReader = new JsonTextReader(textReader))
                repository = serializer.Deserialize<Repository>(jsonReader);

            foreach (Package package in repository.Packages)
                RegisterPackage(package);

            foreach (Uri uri in repository.RepositoryImports)
                workflow.Enqueue(uri, archiveDirectory, RegisterRepository);

            foreach (Uri uri in repository.PackageImports)
                workflow.Enqueue(uri, archiveDirectory, RegisterPackage);
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
                    workflow.Enqueue(archiveUri, archiveDirectory, archiveFilename => UnpackArchive(archiveFilename, downloadUri, filename));
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
                DownloadWorkflow.ExtractFile(archiveFilename, entryName, filename);

                LogSuccess(uri, filename);
            }
        }

        private void RunWorkflow()
        {
            do
            {
                steps++;
                Console.Write("[ {0,3} ] ", steps);
                logNeedsIndent = false;
            } while (workflow.Step());
        }

        protected override int RunCore()
        {
            Directory.CreateDirectory(archivePath);
            workflow.Enqueue(RepositoryUri, archiveDirectory, RegisterRepository);
            RunWorkflow();

            foreach (string name in PackageNames)
            {
                Package package = FindPackage(name);
                DownloadPackageContents(package);
            }

            RunWorkflow();
            return 0;
        }

        public Uri RepositoryUri { get; set; }

        public IList<string> PackageNames
        {
            get { return ExtraArguments; }
        }
    }
}