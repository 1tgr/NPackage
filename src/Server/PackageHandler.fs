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
            match route with
            | { Action = "get"; PackageName = packageName } ->
                let repositoryFilename = "/var/www/np/packages.js"
                let packages = new Uri(repositoryFilename)
                               |> PackageGraph.download Map.empty archiveDirectory
                               |> Download.run

                match Map.tryFind packageName packages with
                | Some package ->
                    context.Response.ContentType <- "application/json"
                    serializer.Serialize(context.Response.Output, package)

                | None -> context.Response.StatusCode <- 404

            | { Action = "list" } ->
                context.Response.ContentType <- "application/json"
                context.Response.Write("hello")

            | _ -> context.Response.StatusCode <- 404

        member this.IsReusable = true
