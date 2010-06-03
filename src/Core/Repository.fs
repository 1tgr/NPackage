namespace NPackage.Core

open System
open System.Collections.Generic

type Repository() =
    let repositoryImports = new ResizeArray<string>()
    let packageImports = new ResizeArray<string>()
    let packages = new ResizeArray<Package>()

    member this.PackageImports = packageImports :> IList<string>
    member this.RepositoryImports = repositoryImports :> IList<string>
    member this.Packages = packages :> IList<Package>
