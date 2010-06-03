module NPackage.Core.PackageGraph

open System
open System.IO
open Newtonsoft.Json

let private serializer = new JsonSerializer()

let private registerPackage packages ({ Package = package } as metadata) =
    let packages' = Map.add (package.Name + "-" + package.Version) { IsAlias = false; Metadata = metadata } packages

    let isNewest =
        match Map.tryFind package.Name packages' with
        | Some { Metadata = { Package = oldPackage } } when oldPackage.Version < package.Version -> true
        | None -> true
        | _ -> false

    if isNewest then
        Map.add package.Name { IsAlias = true; Metadata = metadata } packages'
    else
        packages

let private relativeTo (uri : Uri) (relativeUri : string) =
    new Uri(uri, relativeUri)

let download archiveDirectory = 
    let downloadPackage uri = Download.workflow {
        let! packageFilename = Download.fetch uri archiveDirectory
        use textReader = new StreamReader(packageFilename)
        use jsonReader = new JsonTextReader(textReader)
        let package = serializer.Deserialize<Package>(jsonReader)

        if package.MasterSites.Count = 0 then
            package.MasterSites.Add(uri.GetLeftPart(UriPartial.Path))

        return { Package = package; LastModified = File.GetLastWriteTimeUtc(packageFilename) }
    }

    let rec download' packages uri = Download.workflow {
        let! repositoryFilename = Download.fetch uri archiveDirectory
        use textReader = new StreamReader(repositoryFilename)
        use jsonReader = new JsonTextReader(textReader)
        let repository = serializer.Deserialize<Repository>(jsonReader)

        for package in repository.Packages do
            if package.MasterSites.Count = 0 then
                package.MasterSites.Add(uri.GetLeftPart(UriPartial.Path))

        let! repositoryImports = repository.RepositoryImports
                                 |> List.ofSeq
                                 |> List.map (relativeTo uri >> download' packages)
                                 |> Download.batch

        let! packageImportRefs = repository.PackageImports
                               |> List.ofSeq
                               |> List.map (relativeTo uri >> downloadPackage)
                               |> Download.batch

        let repositoryPackages = repositoryImports
                                 |> List.fold (MapExtensions.appendWith (fun _ value -> value)) packages

        let repositoryLastModified = File.GetLastWriteTimeUtc(repositoryFilename)

        let refs = repository.Packages
                   |> List.ofSeq
                   |> List.map (fun package -> { Package = package; LastModified = repositoryLastModified })

        return List.append refs packageImportRefs
               |> List.fold registerPackage repositoryPackages
    }

    download' Map.empty