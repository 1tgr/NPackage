namespace NPackage.Core

open System
open System.Collections.Generic

type Repository() =
    let repositoryImports = new ResizeArray<Uri>()
    let packageImports = new ResizeArray<Uri>()
    let packages = new ResizeArray<Package>()

    member this.PackageImports = packageImports :> IList<Uri>
    member this.RepositoryImports = repositoryImports :> IList<Uri>
    member this.Packages = packages :> IList<Package>
