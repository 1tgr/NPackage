namespace NPackage.Core

open System

module Download =
    let mutable steps = 0

    let run (DownloadState(m, f) as state) =
        let workflow = new DownloadWorkflow()
        use subscription = workflow.Log.Subscribe(fun (e : LogEventArgs) -> printfn "[%d] %s" steps e.Message)
        Map.iter (fun { Uri = uri; Filename = filename } -> 
            List.iter (fun ref -> 
                match !ref with
                | None -> workflow.Enqueue(uri, filename, fun s -> ref := Some s)
                | Some _ -> ())) m

        steps <- steps + 1
        while workflow.Step() do
            ()

        f ()

    let private succeed x = DownloadState(Map.empty, fun () -> x)

    let rec private merge =
        function
        | [] -> Map.empty
        | [DownloadState(m, _)] -> m
        | DownloadState(m, _) :: tail -> MapExtensions.appendWith List.append m (merge tail)

    let batch_ states =
        DownloadState(merge states, fun () -> List.iter (fun (DownloadState(_, f)) -> f ()) states)

    let batch states =
        DownloadState(merge states, fun () -> List.map (fun (DownloadState(_, f)) -> f ()) states)

    let private bind (DownloadState(m, _) as state) f =
        DownloadState(m, fun () ->
            let v = run state
            run (f v)
        )   
   
    let private delay f = DownloadState(Map.empty, fun () -> run (f()))
 
    let private try_with (DownloadState(m, _) as state) f = DownloadState(m, fun () -> try run state with e -> run (f e))

    let private try_finally (DownloadState(m, _) as state) f = DownloadState(m, fun () -> try run state finally f())

    let private dispose (x : #IDisposable) = x.Dispose()

    let private using r f = try_finally  (f r) (fun () -> dispose r)

    let rec private do_while p state =
        if p() then
            bind state (fun _ -> do_while p state)
        else
            succeed ()

    let fetch uri filename = 
        let r = ref None
        let map = Map.add { Uri = uri; Filename = filename } [r] Map.empty
        DownloadState(map, fun () -> 
            match !r with
            | Some s -> s
            | None -> raise (new InvalidOperationException("Expected a file name for " + uri.ToString() + ".")))

    type DownloadWorkflowBuilder() =
        member b.Bind(state, f) = bind state f

        member b.BindUsing(state, f) = bind state (fun r -> using r f)

        member b.Combine(DownloadState(m1, f1), DownloadState(m2, f2)) =
            DownloadState(MapExtensions.appendWith List.append m1 m2, fun () -> 
                f1 () 
                f2 ())
 
        member b.Delay(f) = f ()

        member b.For (s : #seq<_>, f : ('a -> DownloadState<'b>)) =
            using (s.GetEnumerator()) (fun ie ->
            do_while (fun () -> ie.MoveNext()) (delay (fun() -> f ie.Current))
            )

        member b.Return(x) = succeed x

        member b.ReturnFrom(state) = state

        member b.TryFinally (state, f) = try_finally state f

        member b.TryWith(state, f) = try_with state f

        member b.Using(x : #IDisposable, f) = try_finally (f x) (fun () -> x.Dispose())

        member b.While(p, state : DownloadState<'a>) = do_while p state

        member b.Zero() = succeed ()

    let workflow = new DownloadWorkflowBuilder()
