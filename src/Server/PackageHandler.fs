namespace NPackage.Server

open System.Web

type PackageHandler(route) =
    let { Action = action; Id = id } = route

    interface IHttpHandler with
        member this.ProcessRequest context =
            context.Response.ContentType <- "text/plain"
            context.Response.Output.Write("Hello world: action = {0}, id = {1}", action, id)

        member this.IsReusable =
            true
