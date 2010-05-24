namespace NPackage.Core

open System
open System.Collections.Generic

type HelpCommand(commands : IDictionary<string, Func<string, ICommand>>) =
    let mutable extraArguments = []

    interface ICommand with
        member this.ParseOptions(arguments) =
            extraArguments <- arguments
                              |> List.ofSeq
                              |> List.append extraArguments

        member this.Run() =
            match extraArguments with
            | [] ->
                printfn "Usage:"

                commands.Keys
                |> Seq.iter (printfn "        %s")

                0
            | _ ->
                extraArguments
                |> List.map (fun name ->
                    match commands.TryGetValue(name) with
                    | (true, func) ->
                        match func.Invoke(name) with
                        | :? CommandBase as commandBase -> 
                            commandBase.ShowHelp <- true
                            (commandBase :> ICommand).Run()
                        | _ -> 0
                    | (false, _) -> 1)
                 |> List.max
