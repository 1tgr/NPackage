module NPackage.Core.Download
    let workflow = new DownloadWorkflowBuilder()

    let fetch uri filename = DownloadState [Fetch(uri, filename)]

    let sequence =
        List.map (function
            | DownloadState list -> list)
        >> List.concat
        >> DownloadState

    let rec enqueue (workflow : DownloadWorkflow) (DownloadState list) =
        let rec enqueue' =
            function
            | Fetch(uri, filename) :: xs -> 
                enqueue' xs
            | FetchAndApply(uri, filename, continuation) :: xs ->
                workflow.Enqueue(uri, filename, fun s -> continuation s |> enqueue workflow)
                enqueue' xs
            | [] -> ()
        enqueue' list
