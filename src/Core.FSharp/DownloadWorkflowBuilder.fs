namespace NPackage.Core

type DownloadWorkflowBuilder() =
    member this.Zero() =
        DownloadState []

    member this.Bind(DownloadState actions, continuation) = 
        let makeApply = function
            | Fetch(uri, filename) -> FetchAndApply(uri, filename, continuation)
            | action -> action
        actions
        |> List.map makeApply
        |> DownloadState

    member this.ReturnFrom state =
        state