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
        let archiveDirectory = archivePath + Path.DirectorySeparatorChar.ToString()

        let rec buildGraphByName packages name =
            match Map.tryFind name packages with
            | Some { Metadata = { Package = package } } -> buildGraph packages package
            | None -> raise (new InvalidOperationException("There is no package called " + name + "."))

        and buildGraph packages package =
            let dependentOrder = package.Requires
                                 |> List.ofSeq
                                 |> List.collect (buildGraphByName packages)
                                 |> List.filter (fun p -> p <> package)
            List.append dependentOrder [package]

        let rec installPackageToPath packages packagePath (package : Package) = 
            let downloadLibrary name (library : Library) = Download.workflow {
                let libraryFilename = Path.Combine(packagePath, name)
                let uri = new Uri(new Uri(repositoryUri, package.MasterSites.[0]), library.Binary)

                let builder = new UriBuilder(uri)
                if builder.Fragment.Length > 0 then
                    let fragment = builder.Fragment.TrimStart('#')
                    builder.Fragment <- String.Empty
                    let! archiveFilename = Download.fetch builder.Uri archiveDirectory
                    let archiveFileInfo = new FileInfo(archiveFilename)
                    let libraryFileInfo = new FileInfo(libraryFilename)

                    if (not libraryFileInfo.Exists) || (archiveFileInfo.LastWriteTime > libraryFileInfo.LastWriteTime) then
                        log ("Unpacking " + archiveFilename + "#" + fragment)
                        Archive.extract archiveFilename fragment libraryFilename
                        success libraryFilename
                else
                    do! Download.fetch_ uri libraryFilename
                    success libraryFilename

                return 0
            }

            let copyLocal =
                if package.CopyLocal then
                    buildGraph packages package
                    |> List.filter (fun p -> p <> package)
                    |> List.map (installPackageToPath packages packagePath)
                else
                    [Download.workflow { return 0 }]

            Download.workflow {
                let! codes = package.Libraries
                            |> List.ofSeq
                            |> List.map (fun pair -> downloadLibrary pair.Key pair.Value)
                            |> List.append copyLocal
                            |> Download.batch

                return List.max codes
            }

        let installPackage packages (package : Package) =
            let packagePath = Path.Combine(Path.Combine(libPath, package.Name), package.Version)
            installPackageToPath packages packagePath package            

        let packages = Download.run { PackageGraph.download archiveDirectory repositoryUri with Log = log }
        let installOrder = this.PackageNames
                           |> List.ofSeq
                           |> List.collect (buildGraphByName packages)

        if preview then
            printfn "The following packages would be installed:\n"
            installOrder
            |> List.iter (fun p -> printfn "\t%s version %s" p.Name p.Version)
            0
        else
            { (installOrder
               |> List.map (installPackage packages)
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