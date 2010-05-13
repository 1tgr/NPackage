namespace NPackage.Core

open System

type DownloadState<'a> = DownloadState of Map<UriKey, string ref> * (unit ->'a)