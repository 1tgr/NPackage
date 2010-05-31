module NPackage.Server.RepositoryLock

open System
open System.Threading
open System.Web

let mutable private locks = Map.empty
let private syncObj = new obj()
let timeout = TimeSpan.FromMinutes(1.0)

let private getLock (filename : String) =
    lock syncObj (fun () -> match Map.tryFind filename locks with
                            | Some l -> l
                            | None -> let l = new ReaderWriterLock()
                                      locks <- Map.add filename l locks
                                      l)

let acquireRead filename =
    let rwLock = getLock filename
    rwLock.AcquireReaderLock(timeout)
    { new IDisposable with member this.Dispose() = rwLock.ReleaseReaderLock() }

let acquireWrite filename =
    let rwLock = getLock filename
    rwLock.AcquireWriterLock(timeout)
    { new IDisposable with member this.Dispose() = rwLock.ReleaseWriterLock() }