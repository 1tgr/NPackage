namespace NPackage.Core

open System

type UriKey(uri) =
    override this.Equals(obj) =
        match obj with
        | :? UriKey as k -> uri.Equals(k.Uri)
        | _ -> false

    override this.GetHashCode() =
        uri.GetHashCode()

    interface IComparable with
        member this.CompareTo(obj) =
            match obj with
            | :? UriKey as k -> uri.ToString().CompareTo(k.Uri.ToString())
            | null -> raise (new ArgumentNullException("obj"))
            | x -> raise (new NotSupportedException("Can't compare UriKey with " + x.GetType().Name + "."))

    member this.Uri where
        get = uri

