namespace NPackage.Core

open System

type DownloadAction = Fetch of (Uri * string)
                    | FetchAndApply of (Uri * string * (string -> DownloadState))
and DownloadState = DownloadState of DownloadAction list
