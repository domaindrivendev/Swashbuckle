﻿using System.IO;
using System.Net.Http;
using System.Web.Routing;
using NUnit.Framework;
using Swashbuckle.Core.Handlers;
using Swashbuckle.Models;
using Swashbuckle.Tests.Support;

namespace Swashbuckle.Tests
{
    [TestFixture]
    public class SwaggerUiRouteHandlerTests
    {
        private SwaggerUiRouteHandler _routeHandler;

        [TestFixtureSetUp]
        public void FixtureSetup()
        {
            SwaggerUiConfig.Customize(c =>
                {
                    c.ApiKey = "TestApiKey";
                    c.ApiKeyName = "TestApiKeyName";
                    c.SupportHeaderParams = true;
                    c.SupportedSubmitMethods = new[] {HttpMethod.Get, HttpMethod.Post, HttpMethod.Put, HttpMethod.Head};
                    c.DocExpansion = DocExpansion.Full;
                    c.AddOnCompleteScript(GetType().Assembly, "Swashbuckle.Tests.Support.testScript1.js");
                    c.AddOnCompleteScript(GetType().Assembly, "Swashbuckle.Tests.Support.testScript2.js");
                    c.AddStylesheet(GetType().Assembly, "Swashbuckle.Tests.Support.testStyles1.css");
                    c.AddStylesheet(GetType().Assembly, "Swashbuckle.Tests.Support.testStyles2.css");
                });

            _routeHandler = new SwaggerUiRouteHandler();
        }

        [Test]
        public void It_should_customize_the_swagger_ui_index()
        {
            var responseText = ExecuteRequest("index.html");

            Assert.IsTrue(responseText.Contains("apiKey: \"TestApiKey\""), "apiKey not customized");
            Assert.IsTrue(responseText.Contains("apiKeyName: \"TestApiKeyName\""), "apiKeyName not customized");
            Assert.IsTrue(responseText.Contains("supportHeaderParams: true"), "supportHeaderParams not customized");
            Assert.IsTrue(responseText.Contains("supportedSubmitMethods: ['get','post','put','head']"), "supportedSubmitMethods not customized");
            Assert.IsTrue(responseText.Contains("docExpansion: \"full\""), "docExpansion not customized");
            Assert.IsTrue(responseText.Contains(
                "$.getScript('ext/Swashbuckle.Tests.Support.testScript1.js');\r\n" +
                "$.getScript('ext/Swashbuckle.Tests.Support.testScript2.js');"),
                "CustomScripts not included");
            Assert.IsTrue(responseText.Contains(
                "<link href='ext/Swashbuckle.Tests.Support.testStyles1.css' rel='stylesheet' type='text/css'/>\r\n" +
                "<link href='ext/Swashbuckle.Tests.Support.testStyles2.css' rel='stylesheet' type='text/css'/>"),
                "Stylesheets not included");
        }

        [Test]
        public void It_should_serve_on_complete_scripts()
        {
            var responseText = ExecuteRequest("ext/Swashbuckle.Tests.Support.testScript1.js");
            Assert.IsTrue(responseText.StartsWith("var testVal = '1';"));

            responseText = ExecuteRequest("ext/Swashbuckle.Tests.Support.testScript2.js");
            Assert.IsTrue(responseText.StartsWith("var testVal = '2';"));
        }

        [Test]
        public void It_should_serve_custom_stylesheets()
        {
            var responseText = ExecuteRequest("ext/Swashbuckle.Tests.Support.testStyles1.css");
            Assert.IsTrue(responseText.StartsWith("body {"));

            responseText = ExecuteRequest("ext/Swashbuckle.Tests.Support.testStyles2.css");
            Assert.IsTrue(responseText.StartsWith("h1 {"));
        }

        private string ExecuteRequest(string routePath)
        {
            var httpContext = new FakeHttpContext(routePath);
            var sinkFilter = new MemoryStream();
            httpContext.Response.Filter = sinkFilter;

            var routeData = new RouteData();
            routeData.Values.Add("path", routePath);
            var requestContext = new RequestContext
                {
                    RouteData =  routeData,
                    HttpContext = httpContext
                };

            var httpHandler = _routeHandler.GetHttpHandler(requestContext) as EmbeddedResourceHttpHandler;
            httpHandler.ProcessRequest(httpContext);

            // Simulate output filtering
            var outputStream = httpContext.Response.OutputStream;
            outputStream.Seek(0, SeekOrigin.Begin);
            outputStream.CopyTo(httpContext.Response.Filter);

            sinkFilter.Seek(0, SeekOrigin.Begin);
            using (var reader = new StreamReader(sinkFilter))
            {
                return reader.ReadToEnd();
            }
        }
    }
}