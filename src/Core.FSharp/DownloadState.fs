namespace NPackage.Core

open System

type DownloadState<'a> = { Uris : Map<UriKey, List<Ref<Option<String>>>>;
                           Log : string -> unit;
                           Action : unit ->'a }