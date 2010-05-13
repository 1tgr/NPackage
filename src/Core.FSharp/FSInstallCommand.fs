namespace NPackage.Core

open System
open System.Collections.Generic
open System.IO
open Newtonsoft.Json

type FSInstallCommand() =
    inherit CommandBase()
    let mutable repositoryUri = new Uri("http://np.partario.com/packages.js")
    let serializer = new JsonSerializer()

    override this.CreateOptionSet() =
        base.CreateOptionSet()
            .Add("r|repository", "URL of the packages.js file", fun (v : Uri) -> this.RepositoryUri <- v)

    override this.RunCore() =
        let archivePath = 
            let root = Path.Combine(Path.GetPathRoot(Environment.CurrentDirectory), "lib")
            let rec findLibDirectory path =
                if Directory.Exists(path) then
                    path
                else
                    let fullPath = Path.GetFullPath(path)
                    if String.Equals(root, fullPath, StringComparison.InvariantCultureIgnoreCase) then
                        raise (new InvalidOperationException("Couldn't find lib directory."))
                    else
                        findLibDirectory (Path.Combine("..", path))

            findLibDirectory "lib"

        let archiveDirectory = archivePath + Path.DirectorySeparatorChar.ToString()

        let downloadPackage (package : Package) = 
            let downloadLibrary name (library : Library) = Download.workflow {
                let uri = new Uri(new Uri(package.MasterSites.[0]), library.Binary)
                let! libraryFilename = Download.fetch uri (Path.Combine(archiveDirectory, name))
                printfn "Got %s" libraryFilename
            }

            package.Libraries
            |> List.ofSeq
            |> List.map (fun pair -> downloadLibrary pair.Key pair.Value)
            |> Download.sequence

        let rec buildGraph (map : #IDictionary<string, Package>) uri = Download.workflow {
                let! repositoryFilename = Download.fetch uri archiveDirectory
                use textReader = new StreamReader(repositoryFilename)
                use jsonReader = new JsonTextReader(textReader)
                let repository = serializer.Deserialize<Repository>(jsonReader)

                let! zz = repository.RepositoryImports
                          |> List.ofSeq
                          |> List.map (buildGraph map)
                          |> Download.sequence
                ignore zz

                let addPackages packages map =
                    for package in repository.Packages do
                        map.[package.Name] <- package
                        map.[package.Name + "-" + package.Version] <- package
                addPackages repository.Packages map
            }

        let workflow = new DownloadWorkflow()
        use subscription = workflow.Log.Subscribe(fun (e : LogEventArgs) -> printfn "%s" e.Message)
        let map = new Dictionary<string, Package>()
        Download.enqueue workflow (buildGraph map repositoryUri)
        while workflow.Step() do
            ()
        printfn "Got %d packages" map.Count
        0

    member this.RepositoryUri
        with get() = repositoryUri
        and set(value) = repositoryUri <- value
