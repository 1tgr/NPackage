module NPackage.Core.PackageGraph

open System
open System.IO
open Newtonsoft.Json

let private serializer = new JsonSerializer()

let private downloadPackage archiveDirectory uri = Download.workflow {
    let! packageFilename = Download.fetch uri archiveDirectory
    use textReader = new StreamReader(packageFilename)
    use jsonReader = new JsonTextReader(textReader)
    let package = serializer.Deserialize<Package>(jsonReader)

    if package.MasterSites.Count = 0 then
        package.MasterSites.Add(uri.GetLeftPart(UriPartial.Path))

    return package
}

let private registerPackage packages (package : Package) =
    packages
    |> Map.add package.Name package
    |> Map.add (package.Name + "-" + package.Version) package

let rec download packages archiveDirectory uri = Download.workflow {
    let! repositoryFilename = Download.fetch uri archiveDirectory
    use textReader = new StreamReader(repositoryFilename)
    use jsonReader = new JsonTextReader(textReader)
    let repository = serializer.Deserialize<Repository>(jsonReader)

    for package in repository.Packages do
        if package.MasterSites.Count = 0 then
            package.MasterSites.Add(uri.GetLeftPart(UriPartial.Path))

    let! repositoryImports = repository.RepositoryImports
                                |> List.ofSeq
                                |> List.map (fun relativeUri -> download packages archiveDirectory (new Uri(uri, relativeUri)))
                                |> Download.batch

    let! packageImports = repository.PackageImports
                            |> List.ofSeq
                            |> List.map (fun relativeUri -> downloadPackage archiveDirectory (new Uri(uri, relativeUri)))
                            |> Download.batch

    let packages' = List.fold (MapExtensions.appendWith (fun _ value -> value)) packages repositoryImports
    return packageImports
            |> List.append (List.ofSeq repository.Packages)
            |> List.fold registerPackage packages'
}
