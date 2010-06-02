namespace NPackage.Core

open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Net
open System.Text

type DownloadAction = { Key : UriKey;
                        Continuation : string -> unit }

type DownloadWorkflow(log) =
    let actions = new ResizeArray<DownloadAction>()

    let sanitise (s : String) =
        let invalidPathChars = 
            Path.InvalidPathChars
            |> Seq.append [| Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, ':' |]
            |> Array.ofSeq

        let rec sanitise' (sb : StringBuilder) =
            match sb.ToString().IndexOfAny(invalidPathChars) with
            | n when n < 0 -> sb
            | n ->
                sb.[n] <- '-'
                sanitise' sb

        (sanitise' (new StringBuilder(s))).ToString()

    let ensureFilename (pathOrFilename : string) (uri : Uri) filename =
        match pathOrFilename.EndsWith(Path.DirectorySeparatorChar.ToString()) || pathOrFilename.EndsWith(Path.AltDirectorySeparatorChar.ToString()) with
        | true -> 
            let dir1 =
                let authority = uri.GetLeftPart(UriPartial.Authority)
                if authority = String.Format("{0}://{1}", Uri.UriSchemeHttp, uri.Host) then
                    uri.Host
                else
                    authority

            let dir2 = Path.GetDirectoryName(uri.PathAndQuery.TrimStart('/'))
            Path.Combine(pathOrFilename, Path.Combine(sanitise dir1, Path.Combine(sanitise dir2, filename)))
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
            | Some s -> s
            | None -> Path.GetFileName(response.ResponseUri.GetComponents(UriComponents.Path, UriFormat.Unescaped))
                          
        let responseFilename = ensureFilename filename uri responseUriFilename
        let fileInfo = new FileInfo(responseFilename)

        let lastModified =
            match response with
            | :? HttpWebResponse as httpWebResponse -> httpWebResponse.LastModified
            | :? FileWebResponse as fileWebResponse -> File.GetLastWriteTimeUtc(fileWebResponse.ResponseUri.LocalPath)
            | _ -> DateTime.UtcNow

        if (not fileInfo.Exists) || (lastModified > fileInfo.LastWriteTimeUtc) then
            log (String.Format("Downloading from {0} to {1}", response.ResponseUri, responseFilename))
            using (response.GetResponseStream()) (fun inputStream ->
                let dir = Path.GetDirectoryName(responseFilename)
                ignore (Directory.CreateDirectory(dir))
                use outputStream = File.Create(responseFilename)
                StreamExtensions.copy inputStream outputStream)

            fileInfo.Refresh()
            fileInfo.LastWriteTimeUtc <- lastModified

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
                let firstWriteTime = firstFileInfo.LastWriteTimeUtc
                for (filename, actions) in rest do
                    let responseFilename = ensureFilename filename uri responseUriFilename
                    let fileInfo = new FileInfo(responseFilename)

                    if (not fileInfo.Exists) || firstWriteTime > fileInfo.LastWriteTimeUtc then
                        log (String.Format("Copying from {0} to {1}", firstFilename, responseFilename))
                        let dir = Path.GetDirectoryName(responseFilename)
                        ignore (Directory.CreateDirectory(dir))
                        File.Copy(firstFilename, responseFilename, true)
                        fileInfo.LastWriteTimeUtc <- firstWriteTime

                    for action in actions do
                        action.Continuation responseFilename
            
        actions.Count > 0