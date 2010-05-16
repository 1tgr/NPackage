namespace NPackage.Core

open System
open System.Collections.Generic
open System.IO
open Newtonsoft.Json

type FSInstallCommand() =
    inherit CommandBase()
    let mutable repositoryUri = new Uri("http://np.partario.com/packages.js")
    let serializer = new JsonSerializer()
    let success = printfn "\r ***    Installed %s"
    let log = printfn "    --> %s"

    override this.CreateOptionSet() =
        base.CreateOptionSet()
            .Add("r|repository", "URL of the packages.js file", fun (v : Uri) -> this.RepositoryUri <- v)

    override this.RunCore() =
        let libPath = 
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

        let archivePath = Path.Combine(libPath, ".dist")
        ignore (Directory.CreateDirectory(archivePath))

        let archiveDirectory = archivePath + Path.DirectorySeparatorChar.ToString()

        let downloadPackage uri = Download.workflow {
            let! packageFilename = Download.fetch uri archiveDirectory
            use textReader = new StreamReader(packageFilename)
            use jsonReader = new JsonTextReader(textReader)
            return serializer.Deserialize<Package>(jsonReader)
        }

        let registerPackage packages (package : Package) =
            packages
            |> Map.add package.Name package
            |> Map.add (package.Name + "-" + package.Version) package

        let rec buildGraph packages uri = Download.workflow {
            let! repositoryFilename = Download.fetch uri archiveDirectory
            use textReader = new StreamReader(repositoryFilename)
            use jsonReader = new JsonTextReader(textReader)
            let repository = serializer.Deserialize<Repository>(jsonReader)

            let! repositoryImports = repository.RepositoryImports
                                     |> List.ofSeq
                                     |> List.map (buildGraph packages)
                                     |> Download.batch

            let! packageImports = repository.PackageImports
                                  |> List.ofSeq
                                  |> List.map downloadPackage
                                  |> Download.batch

            let packages' = List.fold (MapExtensions.appendWith (fun _ value -> value)) packages repositoryImports
            return packageImports
                    |> List.append (List.ofSeq repository.Packages)
                    |> List.fold registerPackage packages'
        }

        let installPackage packages name = 
            match Map.tryFind name packages with
            | Some (package : Package) ->
                let packagePath = Path.Combine(Path.Combine(libPath, package.Name), package.Version)
                ignore (Directory.CreateDirectory(packagePath))

                let downloadLibrary name (library : Library) = Download.workflow {
                    let libraryFilename = Path.Combine(packagePath, name)
                    let uri = new Uri(new Uri(package.MasterSites.[0]), library.Binary)

                    let builder = new UriBuilder(uri)
                    if builder.Fragment.Length > 0 then
                        let fragment = builder.Fragment.TrimStart('#')
                        builder.Fragment <- String.Empty
                        let! archiveFilename = Download.fetch builder.Uri archiveDirectory
                        let archiveFileInfo = new FileInfo(archiveFilename)
                        let libraryFileInfo = new FileInfo(libraryFilename)

                        if (not libraryFileInfo.Exists) || (archiveFileInfo.LastWriteTime > libraryFileInfo.LastWriteTime) then
                            log ("Unpacking " + archiveFilename + "#" + fragment)
                            DownloadWorkflow.ExtractFile(archiveFilename, fragment, libraryFilename)
                            success libraryFilename
                    else
                        do! Download.fetch_ uri libraryFilename
                        success libraryFilename
                }

                Download.workflow {
                    do! package.Libraries
                        |> List.ofSeq
                        |> List.map (fun pair -> downloadLibrary pair.Key pair.Value)
                        |> Download.batch_

                    return 0
                }
            | None -> raise (new InvalidOperationException("There is no package called " + name + "."))

        let packages = { buildGraph Map.empty repositoryUri with Log = log }
                       |> Download.run

        { (this.PackageNames
           |> List.ofSeq
           |> List.map (installPackage packages)
           |> Download.batch) with Log = log }
        |> Download.run
        |> List.max

    member this.RepositoryUri
        with get() = repositoryUri
        and set(value) = repositoryUri <- value

    member this.PackageNames
        with get() = base.ExtraArguments
