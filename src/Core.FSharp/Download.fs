namespace NPackage.Core

open System

module Download =
    let run { Uris = m; Log = log; Action = f } =
        let workflow = new DownloadWorkflow()
        use subscription = workflow.Log.Subscribe(fun (e : LogEventArgs) -> log e.Message)
        Map.iter (fun { Uri = uri; Filename = filename } -> 
            List.iter (fun ref -> 
                match !ref with
                | None -> workflow.Enqueue(uri, filename, fun s -> ref := Some s)
                | Some _ -> ())) m

        while workflow.Step() do
            ()

        f ()

    let private no_log = fun _ -> ()

    let private succeed x = { Uris = Map.empty; Log = no_log; Action = fun () -> x }

    let rec private merge =
        function
        | [] -> Map.empty
        | [{ Uris = m }] -> m
        | { Uris = m } :: tail -> MapExtensions.appendWith List.append m (merge tail)

    let batch_ states =
        { Uris = merge states; Log = no_log; Action = fun () -> List.iter (fun { Action = f } -> f ()) states }

    let batch states =
        { Uris = merge states; Log = no_log; Action = fun () -> List.map (fun { Action = f } -> f ()) states }

    let private bind ({ Uris = uris; Log = log } as state) f =
        { Uris = uris; Log = log; Action = fun () -> let v = run state in run (f v) }
   
    let private delay f = 
        { Uris = Map.empty; Log = no_log; Action = fun () -> run (f()) }
 
    let private try_with state f = 
        { state with Action = fun () -> try run state with e -> run (f e) }

    let private try_finally state f = 
        { state with Action = fun () -> try run state finally f() }

    let private dispose (x : #IDisposable) = 
        x.Dispose()

    let private using r f = 
        try_finally  (f r) (fun () -> dispose r)

    let rec private do_while p state =
        if p() then
            bind state (fun () -> do_while p state)
        else
            succeed ()

    let fetch uri filename = 
        let r = ref None
        let map = Map.add { Uri = uri; Filename = filename } [r] Map.empty
        { Uris = map; Log = no_log; Action = fun () -> 
            match !r with
            | Some s -> s
            | None -> raise (new InvalidOperationException("Expected a file name for " + uri.ToString() + ".")) }

    let fetch_ uri filename = 
        { Uris = Map.add { Uri = uri; Filename = filename } [] Map.empty; Log = no_log; Action = id }

    type DownloadWorkflowBuilder() =
        member b.Bind(state, f) = bind state f

        member b.BindUsing(state, f) = bind state (fun r -> using r f)

        member b.Combine({ Uris = m1; Action = f1 }, { Uris = m2; Log = log2; Action = f2 }) =
            { Uris = MapExtensions.appendWith List.append m1 m2; Log = log2; Action = fun () -> 
                f1 () 
                f2 () }
 
        member b.Delay(f) = f ()

        member b.For (s : #seq<_>, f) =
            using (s.GetEnumerator()) (fun ie ->
            do_while (fun () -> ie.MoveNext()) (delay (fun() -> f ie.Current))
            )

        member b.Return(x) = succeed x

        member b.ReturnFrom(state) = state

        member b.TryFinally (state, f) = try_finally state f

        member b.TryWith(state, f) = try_with state f

        member b.Using(x : #IDisposable, f) = try_finally (f x) (fun () -> x.Dispose())

        member b.While(p, state) = do_while p state

        member b.Zero() = succeed ()

    let workflow = new DownloadWorkflowBuilder()
