namespace NPackage.Server

open System.Web
open System.Web.Routing
open Microsoft.FSharp.Reflection

module Application =
    let addRoute<'Route, 'Handler when 'Handler :> IHttpHandler> url (defaults : 'Route) (makeHandler : 'Route -> 'Handler) (routes : RouteCollection) =
        routes.Add(new Route(url, new RouteValueDictionary(defaults), CustomRouteHandler.create makeHandler))
        routes

    let registerRoutes =
        //addRoute "packages" { PackageName = ""; Action = "list" } (fun route -> new PackageHandler(route)) >>
        addRoute "packages/{PackageName}/{Action}" { PackageName = ""; Action = "get" } (fun route -> new PackageHandler(route))

type ApplicationType() =
    inherit HttpApplication()

    member this.Application_Start() =
        Application.registerRoutes RouteTable.Routes
