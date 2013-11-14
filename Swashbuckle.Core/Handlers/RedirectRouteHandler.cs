﻿using System.Web;
using System.Web.Routing;
using Swashbuckle.Core.Models;

namespace Swashbuckle.Core.Handlers
{
    public class RedirectRouteHandler : IRouteHandler
    {
        private readonly string _redirectUrl;

        public RedirectRouteHandler(string redirectUrl)
        {
            _redirectUrl = redirectUrl;
        }

        public IHttpHandler GetHttpHandler(RequestContext requestContext)
        {
            return new RedirectHttpHandler(_redirectUrl);
        }
    }

   

    public class RedirectHttpHandler : IHttpHandler
    {
        private readonly string _redirectUrl;

        public RedirectHttpHandler(string redirectUrl)
        {
            _redirectUrl = redirectUrl;
        }

        public void ProcessRequest(HttpContext context)
        {
            var baseUrl = SwaggerSpecConfig.Instance.BasePathResolver();
            if (!baseUrl.EndsWith("/"))
                baseUrl += "/";
            var relativeUrl = _redirectUrl;
            if (relativeUrl.StartsWith("/"))
                relativeUrl = relativeUrl.Substring(1);
            
            context.Response.Redirect(baseUrl + relativeUrl);
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }

    public class RouteDirectionConstraint : IRouteConstraint
    {
        private readonly RouteDirection _direction;

        public RouteDirectionConstraint(RouteDirection direction)
        {
            _direction = direction;
        }

        public bool Match(HttpContextBase httpContext, Route route, string parameterName,
            RouteValueDictionary values, RouteDirection routeDirection)
        {
            return routeDirection == _direction;
        }
    }
    
}