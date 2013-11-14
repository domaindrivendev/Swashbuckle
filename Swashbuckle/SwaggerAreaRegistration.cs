﻿using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Swashbuckle.Core.Handlers;

namespace Swashbuckle
{
    public class SwaggerAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get { return "Swagger"; }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.Routes.MapRoute(
                "swagger_declaration",
                "swagger/api-docs/{resourceName}",
                new { controller = "ApiDocs", action = "Show" });

            context.Routes.MapRoute(
                "swagger_listing",
                "swagger/api-docs",
                new {controller = "ApiDocs", action = "Index"});

            context.Routes.Add(new Route(
                "swagger",
                null,
                new RouteValueDictionary(new {constraint = new RouteDirectionConstraint(RouteDirection.IncomingRequest)}),
                new RedirectRouteHandler("swagger/ui/index.html")));

            context.Routes.Add(new Route(
                "swagger/ui/{*path}",
                null,
                new RouteValueDictionary(new {constraint = new RouteDirectionConstraint(RouteDirection.IncomingRequest)}),
                new SwaggerUiRouteHandler()));
        }
    }

    
}