namespace NPackage.Core

open System

type DownloadState<'a> = DownloadState of Map<UriKey, List<Ref<Option<String>>>> * (unit ->'a)