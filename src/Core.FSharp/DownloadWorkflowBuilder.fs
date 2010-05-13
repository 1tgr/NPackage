namespace NPackage.Core

open System

type DownloadWorkflowBuilder() =
    let bind (DownloadState actions) continuation = 
        let makeApply = function
            | Fetch(uri, filename) -> FetchAndApply(uri, filename, continuation)
            | action -> action
        actions
        |> List.map makeApply
        |> DownloadState

    let rec do_while p m =
        if p() then
            bind m (fun _ -> do_while p m)
        else
            DownloadState []

    member this.Zero() =
        DownloadState []

    member this.Bind(m, f) = 
        bind m f

    member this.ReturnFrom state =
        state

    member this.While(p, m) = do_while p m