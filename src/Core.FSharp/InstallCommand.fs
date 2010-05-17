namespace NPackage.Core

open System
open System.Collections.Generic
open System.IO
open Newtonsoft.Json

type InstallCommand() =
    inherit CommandBase()
    let mutable repositoryUri = new Uri("http://np.partario.com/packages.js")
    let mutable preview = false
    let baseUri = new Uri(Environment.CurrentDirectory + Path.DirectorySeparatorChar.ToString())
    let serializer = new JsonSerializer()
    let success = printfn "\r ***    Installed %s"
    let log = printfn "    --> %s"

    override this.CreateOptionSet() =
        base.CreateOptionSet()
            .Add("p|preview", "Only show what packages would be installed; don't install them", function
                                                                                                | null -> preview <- false
                                                                                                | _ -> preview <- true)
            .Add("r|repository=", "URL of the packages.js file", fun (v : string) -> this.RepositoryUri <- new Uri(baseUri, v))

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
            let package = serializer.Deserialize<Package>(jsonReader)

            if package.MasterSites.Count = 0 then
                package.MasterSites.Add(uri.GetLeftPart(UriPartial.Path))

            return package
        }

        let registerPackage packages (package : Package) =
            packages
            |> Map.add package.Name package
            |> Map.add (package.Name + "-" + package.Version) package

        let rec downloadPackages packages uri = Download.workflow {
            let! repositoryFilename = Download.fetch uri archiveDirectory
            use textReader = new StreamReader(repositoryFilename)
            use jsonReader = new JsonTextReader(textReader)
            let repository = serializer.Deserialize<Repository>(jsonReader)

            for package in repository.Packages do
                if package.MasterSites.Count = 0 then
                    package.MasterSites.Add(uri.GetLeftPart(UriPartial.Path))

            let! repositoryImports = repository.RepositoryImports
                                     |> List.ofSeq
                                     |> List.map (fun relativeUri -> downloadPackages packages (new Uri(uri, relativeUri)))
                                     |> Download.batch

            let! packageImports = repository.PackageImports
                                  |> List.ofSeq
                                  |> List.map (fun relativeUri -> downloadPackage (new Uri(uri, relativeUri)))
                                  |> Download.batch

            let packages' = List.fold (MapExtensions.appendWith (fun _ value -> value)) packages repositoryImports
            return packageImports
                    |> List.append (List.ofSeq repository.Packages)
                    |> List.fold registerPackage packages'
        }

        let installPackage (package : Package) = 
            let packagePath = Path.Combine(Path.Combine(libPath, package.Name), package.Version)

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

            ignore (Directory.CreateDirectory(packagePath))
            Download.workflow {
                do! package.Libraries
                    |> List.ofSeq
                    |> List.map (fun pair -> downloadLibrary pair.Key pair.Value)
                    |> Download.batch_

                return 0
            }

        let rec buildGraph packages name =
            match Map.tryFind name packages with
            | Some (package : Package) -> 
                let dependentOrder = package.Requires
                                     |> List.ofSeq
                                     |> List.collect (buildGraph packages)
                                     |> List.filter (fun p -> p <> package)
                List.append dependentOrder [package]

            | None -> raise (new InvalidOperationException("There is no package called " + name + "."))

        let packages = Download.run { downloadPackages Map.empty repositoryUri with Log = log }
        let installOrder = this.PackageNames
                           |> List.ofSeq
                           |> List.collect (buildGraph packages)

        if preview then
            printfn "The following packages would be installed:\n"
            installOrder
            |> List.iter (fun p -> printfn "\t%s version %s" p.Name p.Version)
            0
        else
            { (installOrder
               |> List.map installPackage
               |> Download.batch) with Log = log }
            |> Download.run
            |> List.max

    member this.RepositoryUri
        with get() = repositoryUri
        and set(value) = repositoryUri <- value

    member this.PackageNames
        with get() = base.ExtraArguments

    member this.Preview
        with get() = preview
        and set(value) = preview <- value