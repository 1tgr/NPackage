namespace NPackage.Core

open System.Collections.Generic

type ICommand =
    abstract ParseOptions : IEnumerable<string> -> unit
    abstract Run : unit -> int