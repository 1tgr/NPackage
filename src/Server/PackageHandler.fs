namespace NPackage.Server

open System
open System.IO
open System.Web
open Newtonsoft.Json
open NPackage.Core

type PackageHandler(route) =
    let serializer = new JsonSerializer()
    let archivePath = Path.Combine(Path.GetTempPath(), "NPackage")
    let archiveDirectory = archivePath + Path.DirectorySeparatorChar.ToString()
    do ignore (Directory.CreateDirectory(archivePath))

    interface IHttpHandler with
        member this.ProcessRequest context =
            let repositoryFilename = context.Request.MapPath("~/packages.js")
            use l = RepositoryLock.acquireRead repositoryFilename
            let packages = new Uri(repositoryFilename)
                            |> PackageGraph.download archiveDirectory
                            |> Download.run

            use outputWriter = new JsonTextWriter(context.Response.Output)
            outputWriter.Formatting <- Formatting.Indented
                
            match route with
            | { Action = "get"; PackageName = packageName } ->
                match Map.tryFind packageName packages with
                | Some { Metadata = { Package = package; LastModified = lastModified } } ->
                    context.Response.ContentType <- "application/json"
                    context.Response.Cache.SetLastModified(lastModified)
                    serializer.Serialize(outputWriter, package)

                | None -> context.Response.StatusCode <- 404

            | { Action = "list" } ->
                let packageList = packages 
                                  |> Map.toList
                                  |> List.filter (function
                                                  | (_, { IsAlias = false }) -> true
                                                  | (_, { IsAlias = true }) -> false)

                let lastModified = packageList
                                   |> List.map (fun (_, { Metadata = { LastModified = lastModified }}) -> lastModified)
                                   |> List.max

                let repository = new Repository()
                for (_, { Metadata = { Package = package }}) in packageList do
                    repository.Packages.Add(package)

                context.Response.ContentType <- "application/json"
                context.Response.Cache.SetLastModified(lastModified)
                serializer.Serialize(outputWriter, repository)

            | _ -> context.Response.StatusCode <- 404

        member this.IsReusable = true
