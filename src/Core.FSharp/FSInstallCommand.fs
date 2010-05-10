namespace NPackage.Core

open System
open System.IO
open Newtonsoft.Json

type FSInstallCommand() =
    inherit CommandBase()
    let mutable repositoryUri = new Uri("http://np.partario.com/packages.js")

    override this.CreateOptionSet() =
        base.CreateOptionSet()
            .Add("r|repository", "URL of the packages.js file", fun (v : Uri) -> this.RepositoryUri <- v)

    override this.RunCore() =
        let archiveDirectory = 
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

        let downloadLibrary (package : Package) name (library : Library) = Download.workflow {
                let uri = new Uri(new Uri(package.MasterSites.[0]), library.Binary)
                let! libraryFilename = Download.fetch uri (Path.Combine(archiveDirectory, name))
                printfn "Got %s" libraryFilename
            }

        let downloadPackage (package : Package) = 
            package.Libraries
            |> List.ofSeq
            |> Download.map (fun pair -> downloadLibrary package pair.Key pair.Value)

        let b = Download.workflow {
                let! repositoryFilename = Download.fetch repositoryUri archiveDirectory
                let textReader = new StreamReader(repositoryFilename)
                let jsonReader = new JsonTextReader(textReader)
                let serializer = new JsonSerializer()
                let repository = serializer.Deserialize<Repository>(jsonReader)
                return! Download.map downloadPackage (List.ofSeq repository.Packages)
            }

        let workflow = new DownloadWorkflow()
        use subscription = workflow.Log.Subscribe(fun (e : LogEventArgs) -> printfn "%s" e.Message)
        Download.enqueue workflow b
        while workflow.Step() do
            ()
        0

    member this.RepositoryUri
        with get() = repositoryUri
        and set(value) = repositoryUri <- value
