namespace NPackage.Core
open System

module Download =
    let run (DownloadState f) = f ()

    type DownloadWorkflowBuilder() =
        let succeed x = DownloadState (fun () -> x)

        let bind m f =
            DownloadState (fun () ->
                let v = run m
                run (f v)
            )   
   
        let delay f = DownloadState (fun () -> run (f()))
 
        let try_with m f = DownloadState (fun () -> try run m with e -> run (f e))

        let try_finally m f = DownloadState (fun () -> try run m finally f())

        let dispose (x: #IDisposable) = x.Dispose()

        let using r f = try_finally  (f r) (fun () -> dispose r)

        let rec do_while p m =
            if p() then
                bind m (fun _ -> do_while p m)
            else
                succeed ()

        member b.Bind(m, f) = bind m f

        member b.BindUsing(m, f) = bind m (fun r -> using r f)

        member b.Combine(m1, m2) = bind m1 (fun () -> m2)
 
        member b.Delay(f) = delay f

        member b.For (s : #seq<_>, f : ('a -> DownloadState<'b>)) =
            using (s.GetEnumerator()) (fun ie ->
            do_while (fun () -> ie.MoveNext()) (delay (fun() -> f ie.Current))
            )

        //    member b.Let(x, f) = f x

        member b.Return(x) = succeed x

        member b.TryFinally (m, f) = try_finally m f

        member b.TryWith(m, f) = try_with m f

        member b.Using(x : #IDisposable, f) = try_finally (f x) (fun () -> x.Dispose())

        member b.While(p, m : DownloadState<'a>) = do_while p m

        member b.Zero() = succeed ()

    let workflow = new DownloadWorkflowBuilder()
