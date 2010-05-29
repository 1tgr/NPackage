namespace NPackage.Server

open System.Web
open System.Web.Routing
open Microsoft.FSharp.Reflection

module Application =
    let registerRoutes (routes : RouteCollection) =
        routes.Add(
            new Route("{Action}/{Id}", 
                CustomRouteHandler.create (fun route -> new PackageHandler(route)), 
                Defaults = new RouteValueDictionary({ Action = "index"; Id = "" })))

type ApplicationType() =
    inherit HttpApplication()

    member this.Application_Start() =
        Application.registerRoutes RouteTable.Routes
