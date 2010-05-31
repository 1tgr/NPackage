namespace NPackage.Server

open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Web
open Newtonsoft.Json
open NPackage.Core

type AdminHandler(route : AdminRoute) =
    let serializer = new JsonSerializer()

    let rec removeDuplicates (package : Package) (packages : IList<Package>) =
        match packages |> Seq.tryFindIndex (fun p -> p.Name = package.Name && p.Version = package.Version) with
        | Some index ->
            packages.RemoveAt(index)
            removeDuplicates package packages

        | None -> ()

    interface IHttpHandler with
        member this.ProcessRequest context =
            let repositoryFilename = context.Request.MapPath("~/packages.js")
            use l = RepositoryLock.acquireWrite repositoryFilename
            let repository = using (File.OpenText(repositoryFilename))
                                   (fun streamReader -> use jsonReader = new JsonTextReader(streamReader)
                                                        serializer.Deserialize<Repository>(jsonReader))

            use outputWriter = new JsonTextWriter(context.Response.Output)
            outputWriter.Formatting <- Formatting.Indented

            match route with
            | { Action = "submit" } ->
                match context.Request.Form.["json"] with
                | s when not (String.IsNullOrEmpty(s)) ->
                    use stringReader = new StringReader(s)
                    use jsonReader = new JsonTextReader(stringReader)
                    let package = serializer.Deserialize<Package>(jsonReader)

                    if String.IsNullOrEmpty(package.Name) then
                        raise (new InvalidOperationException("'Name' field is blank."))

                    if String.IsNullOrEmpty(package.Version) then
                        raise (new InvalidOperationException("'Version' field is blank."))

                    removeDuplicates package repository.Packages
                    repository.Packages.Add(package)
                    use streamWriter = File.CreateText(repositoryFilename)
                    use jsonWriter = new JsonTextWriter(streamWriter)
                    jsonWriter.Formatting <- Formatting.Indented
                    serializer.Serialize(jsonWriter, repository)
                    serializer.Serialize(outputWriter, package)

                    try
                        use p = Process.Start(context.Request.MapPath("~/after-submit"), String.Format(@"""{0}"" ""{1}""", package.Name, package.Version))
                        p.WaitForExit()
                    with _ -> ()

                | _ -> raise (new InvalidOperationException("Expected a form field called 'json'."))

            | _ -> context.Response.StatusCode <- 404

        member this.IsReusable = true
