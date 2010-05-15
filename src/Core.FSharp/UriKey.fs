namespace NPackage.Core

open System

type UriKey = { Uri : Uri; Filename : string }
    with
        interface IComparable with
            member this.CompareTo(obj) =
                match obj with
                | :? UriKey as k -> 
                    match this.Uri.ToString().CompareTo(k.Uri.ToString()) with
                    | 0 -> this.Filename.CompareTo(k.Filename)
                    | n -> n
                | null -> raise (new ArgumentNullException("obj"))
                | x -> raise (new NotSupportedException("Can't compare UriKey with " + x.GetType().Name + "."))
