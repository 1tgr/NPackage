namespace NPackage.Core

open System
open System.Collections.Generic
open Mono.Options

[<AbstractClass>]
type CommandBase() =
    let extraArguments = new ResizeArray<string>()
    let mutable showHelp = false

    abstract CreateOptionSet : unit -> OptionSet
    abstract RunCore : unit -> int

    default this.CreateOptionSet() =
        (new OptionSet())
            .Add("h|help", "show this message and exit", function
                                                         | null -> showHelp <- false
                                                         | _ -> showHelp <- true)


    member this.ExtraArguments = extraArguments :> IList<string>

    member this.ShowHelp
        with get() = showHelp
        and set(value) = showHelp <- value

    interface ICommand with
        member this.ParseOptions(arguments) =
            let set = this.CreateOptionSet()
            extraArguments.AddRange(set.Parse(arguments))

        member this.Run() =
            if showHelp then
                printfn "Usage:"
                this.CreateOptionSet().WriteOptionDescriptions(Console.Out)
                0
            else
                this.RunCore()
