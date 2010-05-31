namespace NPackage.Core

open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Net

type DownloadAction = { Key : UriKey;
                        Continuation : string -> unit }

type DownloadWorkflow(log) =
    let actions = new ResizeArray<DownloadAction>()

    let addFilename (pathOrFilename : string) filename =
        match pathOrFilename.EndsWith(Path.DirectorySeparatorChar.ToString()) || pathOrFilename.EndsWith(Path.AltDirectorySeparatorChar.ToString()) with
        | true -> Path.Combine(pathOrFilename, filename)
        | false -> pathOrFilename

    let attachmentFilename (response : WebResponse) =
        let contentDisposition = response.Headers.["Content-Disposition"]
        if String.IsNullOrEmpty(contentDisposition) then
            None
        else
            let parts = contentDisposition.Split([| ';' |])
            if parts.Length > 1 then
                let part1 = parts.[1].TrimStart()
                let prefix = "filename="
                if part1.StartsWith(prefix) then
                    Some (part1.Substring(prefix.Length).Trim('"').Trim())
                else
                    None
            else
                None

    let downloadFirst (uri : Uri) filename actions =
        log (String.Format("Checking {0}", uri))

        let request = WebRequest.Create(uri)
        match request with
        | :? HttpWebRequest as httpWebRequest -> 
            try
                httpWebRequest.UseDefaultCredentials <- true
            with :? NotImplementedException ->
                ()

        | _ -> ()

        use response = request.GetResponse()
        let responseUriFilename =
            match attachmentFilename response with
            | Some filename -> filename
            | None -> Path.GetFileName(response.ResponseUri.GetComponents(UriComponents.Path, UriFormat.Unescaped))
                          
        let responseFilename = addFilename filename responseUriFilename
        let fileInfo = new FileInfo(responseFilename)

        let httpWebResponse = 
            match response with
            | :? HttpWebResponse as r -> r
            | _ -> null

        if (not fileInfo.Exists) || (httpWebResponse <> null && httpWebResponse.LastModified > fileInfo.LastWriteTime) then
            log (String.Format("Downloading from {0} to {1}", response.ResponseUri, responseFilename))
            using (response.GetResponseStream()) (fun inputStream ->
                using (File.Create(responseFilename)) (StreamExtensions.copy inputStream))

            fileInfo.Refresh()

            if httpWebResponse <> null then
                fileInfo.LastWriteTime <- httpWebResponse.LastModified
                            
        for action in actions do
            action.Continuation responseFilename

        (responseFilename, fileInfo, responseUriFilename)

    member this.Enqueue(key, continuation) =
        actions.Add({ Key = key; Continuation = continuation })

    member this.Step() =
        let actionsByFilenameByUri = 
            actions
            |> Seq.groupBy (fun { Key = { Uri = uri } } -> uri)
            |> Seq.map (fun (key, g) -> 
                (key, g
                        |> Seq.groupBy (fun { Key = { Filename = filename } } -> filename)
                        |> Seq.map (fun (key', g') -> (key', List.ofSeq g'))
                        |> List.ofSeq
                        |> List.sortBy fst))
            |> List.ofSeq
            |> List.sortBy (fun (key, _) -> key.ToString())

        actions.Clear()

        for (uri, actionsByFilename) in actionsByFilenameByUri do
            match actionsByFilename with
            | [] -> ()
            | [(filename, actions)] -> ignore (downloadFirst uri filename actions)
            | (filename, actions) :: rest ->
                let (firstFilename, firstFileInfo, responseUriFilename) = downloadFirst uri filename actions
                for (filename, actions) in rest do
                    let responseFilename = addFilename filename responseUriFilename
                    let fileInfo = new FileInfo(responseFilename)

                    if (not fileInfo.Exists) || firstFileInfo.LastWriteTime > fileInfo.LastWriteTime then
                        log (String.Format("Copying from {0} to {1}", firstFilename, responseFilename))
                        File.Copy(firstFilename, responseFilename, true)
                        fileInfo.LastWriteTime <- firstFileInfo.LastWriteTime

                    for action in actions do
                        action.Continuation responseFilename
            
        actions.Count > 0