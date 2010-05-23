namespace NPackage.Core

open System

type Library() =
    let mutable binary = String.Empty

    member this.Binary
        with get() = binary
        and set(value) = binary <- value
