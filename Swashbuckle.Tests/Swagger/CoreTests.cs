using Newtonsoft.Json.Linq;
using System;
using NUnit.Framework;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Linq;
using Swashbuckle.Application;
using Swashbuckle.Dummy;
using Swashbuckle.Dummy.Controllers;
using Swashbuckle.Dummy.SwaggerExtensions;

namespace Swashbuckle.Tests.Swagger
{
    [TestFixture]
    public class CoreTests : SwaggerTestBase
    {
        public CoreTests()
            : base("swagger/docs/{apiVersion}")
        { }

        [SetUp]
        public void SetUp()
        {
            // Default set-up
            SetUpHandler();
        }

        [Test]
        public void It_provides_swagger_version_2_0()
        {
            var swagger = GetContent<JObject>("http://tempuri.org/swagger/docs/v1");

            Assert.AreEqual("2.0", swagger["swagger"].ToString());
        }

        [Test]
        public void It_provides_info_version_and_title()
        {
            var swagger = GetContent<JObject>("http://tempuri.org/swagger/docs/v1");

            var info = swagger["info"];
            Assert.IsNotNull(info);

            var expected = JObject.FromObject(new
                {
                    version = "v1",
                    title = "Test API",
                });
            Assert.AreEqual(expected.ToString(), info.ToString());
        }

        [Test]
        public void It_provides_host_base_path_and_default_schemes()
        {
            var swagger = GetContent<JObject>("http://tempuri.org:1234/swagger/docs/v1");

            var host = swagger["host"];
            Assert.AreEqual("tempuri.org:1234", host.ToString());

            var basePath = swagger["basePath"];
            Assert.IsNull(basePath);

            var schemes = swagger["schemes"];
            var expected = JArray.FromObject(new[] { "http" });
            Assert.AreEqual(expected, schemes);
        }

        [Test]
        public void It_provides_base_path_from_virtual_directory()
        {
            // When there is a virtual directory
            Configuration = new HttpConfiguration(new HttpRouteCollection("/foobar"));
            var swagger = GetContent<JObject>("http://tempuri.org:1234/swagger/docs/v1");

            var basePath = swagger["basePath"];
            Assert.AreEqual("/foobar", basePath.ToString());
        }
        
        [Test]
        public void It_provides_a_description_for_each_path_in_the_api()
        {
            SetUpDefaultRoutesFor(new[] { typeof(ProductsController), typeof(CustomersController) });

            var swagger = GetContent<JObject>("http://tempuri.org/swagger/docs/v1");
            var paths = swagger["paths"];

            var expected = JObject.FromObject(new Dictionary<string, object>
                {
                    {
                        "/customers", new
                        {
                            post = new
                            {
                                tags = new [] { "Customers" },
                                operationId = "Customers_Create",
                                consumes = new []{ "application/json", "text/json", "application/xml", "text/xml", "application/x-www-form-urlencoded" },
                                produces = new []{ "application/json", "text/json", "application/xml", "text/xml" },
                                parameters = new []
                                {
                                    new
                                    {
                                        name = "customer",
                                        @in = "body",
                                        required = true,
                                        schema = JObject.Parse("{ $ref: \"#/definitions/Customer\" }")
                                    }
                                },
                                responses = new Dictionary<string, object>
                                {
                                    {
                                        "200", new
                                        {
                                            description = "OK",
                                            schema = new
                                            {
                                                format = "int32",
                                                type = "integer"
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    },
                    {
                        "/customers/{id}", new
                        {
                            put = new
                            {
                                tags = new [] { "Customers" },
                                operationId = "Customers_Update",
                                consumes = new []{ "application/json", "text/json", "application/xml", "text/xml", "application/x-www-form-urlencoded" },
                                produces = new string[]{},
                                parameters = new object []
                                {
                                    new
                                    {
                                        name = "id",
                                        @in = "path",
                                        required = true,
                                        type = "integer",
                                        format = "int32",
                                    },
                                    new
                                    {
                                        name = "customer",
                                        @in = "body",
                                        required = true,
                                        schema = JObject.Parse("{ $ref: \"#/definitions/Customer\" }")
                                    }
                                },
                                responses = new Dictionary<string, object>
                                {
                                    {
                                        "204", new
                                        {
                                            description = "No Content",
                                        }
                                    }
                                }
                            },
                            delete = new
                            {
                                tags = new [] { "Customers" },
                                operationId = "Customers_Delete",
                                consumes = new string[]{},
                                produces = new string[]{},
                                parameters = new []
                                {
                                    new
                                    {
                                        name = "id",
                                        @in = "path",
                                        required = true,
                                        type = "integer",
                                        format = "int32"
                                    }
                                },
                                responses = new Dictionary<string, object>
                                {
                                    {
                                        "204", new
                                        {
                                            description = "No Content",
                                        }
                                    }
                                }
                            }
                        }
                    },
                    {
                        "/products", new
                        {
                            get = new
                            {
                                tags = new [] { "Products" },
                                operationId = "Products_GetAllByType",
                                consumes = new object[]{},
                                produces = new []{ "application/json", "text/json", "application/xml", "text/xml" },
                                parameters = new []
                                {
                                    new
                                    {
                                        name = "type",
                                        @in = "query",
                                        required = true,
                                        type = "integer",
                                        format = "int32",
                                        @enum = new[] { 2, 4 }
                                    }
                                },
                                responses = new Dictionary<string, object>
                                {
                                    {
                                        "200", new
                                        {
                                            description = "OK",
                                            schema = new
                                            {
                                                type = "array",
                                                items = JObject.Parse("{ $ref: \"#/definitions/Product\" }"),
                                                xml = JObject.Parse( "{ \"name\": \"Product\", \"wrapped\": true }" )
                                            }
                                        }
                                    }
                                }
                            },
                            post = new
                            {
                                tags = new [] { "Products" },
                                operationId = "Products_Create",
                                consumes = new []{ "application/json", "text/json", "application/xml", "text/xml", "application/x-www-form-urlencoded" },
                                produces = new []{ "application/json", "text/json", "application/xml", "text/xml" },
                                parameters = new []
                                {
                                    new
                                    {
                                        name = "product",
                                        @in = "body",
                                        required = true,
                                        schema = JObject.Parse("{ $ref: \"#/definitions/Product\" }")
                                    }
                                },
                                responses = new Dictionary<string, object>
                                {
                                    {
                                        "200", new
                                        {
                                            description = "OK",
                                            schema = new
                                            {
                                                format = "int32",
                                                type = "integer"
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    },
                    {
                        "/products/{id}", new
                        {
                            get = new
                            {
                                tags = new [] { "Products" },
                                operationId = "Products_GetById",
                                consumes = new object[]{},
                                produces = new []{ "application/json", "text/json", "application/xml", "text/xml" },
                                parameters = new []
                                {
                                    new
                                    {
                                        name = "id",
                                        @in = "path",
                                        required = true,
                                        type = "integer",
                                        format = "int32"
                                    }
                                },
                                responses = new Dictionary<string, object>
                                {
                                    {
                                        "200", new
                                        {
                                            description = "OK",
                                            schema = JObject.Parse("{ $ref: \"#/definitions/Product\" }")
                                        }
                                    }
                                }
                            }
                        }
                    }
                });

            Assert.AreEqual(expected.ToString(), paths.ToString());
        }

        [Test]
        public void It_sets_the_deprecated_flag_on_actions_that_are_obsolete()
        {
            SetUpDefaultRouteFor<ObsoleteActionsController>();

            var swagger = GetContent<JObject>("http://tempuri.org/swagger/docs/v1");
            var putOp = swagger["paths"]["/obsoleteactions/{id}"]["put"];
            var deleteOp = swagger["paths"]["/obsoleteactions/{id}"]["delete"];

            Assert.IsNull(putOp["deprecated"]);
            Assert.IsTrue((bool)deleteOp["deprecated"]);
        }

        [Test]
        public void It_exposes_config_to_include_additional_info_properties()
        {
            SetUpHandler(c =>
                {
                    c.SingleApiVersion("v1", "Test API")
                        .Description("A test API")
                        .TermsOfService("Test terms")
                        .Contact(cc => cc
                            .Name("Joe Test")
                            .Url("http://tempuri.org/contact")
                            .Email("joe.test@tempuri.org"))
                        .License(lc => lc
                            .Name("Test License")
                            .Url("http://tempuri.org/license"));
                });

            var swagger = GetContent<JObject>("http://tempuri.org/swagger/docs/v1");

            var info = swagger["info"];
            Assert.IsNotNull(info);

            var expected = JObject.FromObject(new
                {
                    version = "v1",
                    title = "Test API",
                    description = "A test API",
                    termsOfService = "Test terms",
                    contact = new
                    {
                        name = "Joe Test",
                        url = "http://tempuri.org/contact",
                        email = "joe.test@tempuri.org"
                    },
                    license = new
                    {
                        name = "Test License",
                        url = "http://tempuri.org/license"
                    }
                });
            Assert.AreEqual(expected.ToString(), info.ToString());
        }

        [Test]
        public void It_exposes_config_to_explictly_provide_supported_schemes()
        {
            SetUpHandler(c => c.Schemes(new[] { "http", "https" }));

            var swagger = GetContent<JObject>("http://tempuri.org/swagger/docs/v1");
            var schemes = swagger["schemes"];
            var expected = JArray.FromObject(new[] { "http", "https" });

            Assert.AreEqual(expected.ToString(), schemes.ToString());
        }

        [Test]
        public void It_exposes_config_to_pretty_print_docs_output()
        {
            SetUpDefaultRouteFor<ObsoleteActionsController>();

            var swagger = GetContentAsString("http://tempuri.org/swagger/docs/v1");
            Assert.IsTrue(swagger.StartsWith("{\"swagger\":"));

            SetUpHandler(c => c.PrettyPrint());

            swagger = GetContentAsString("http://tempuri.org/swagger/docs/v1");
            Assert.IsTrue(swagger.StartsWith("{\r\n  \"swagger\":"));
        }

        [Test]
        public void It_exposes_config_to_ignore_all_actions_that_are_obsolete()
        {
            SetUpDefaultRouteFor<ObsoleteActionsController>();
            SetUpHandler(c => c.IgnoreObsoleteActions());

            var swagger = GetContent<JObject>("http://tempuri.org/swagger/docs/v1");
            var putOp = swagger["paths"]["/obsoleteactions/{id}"]["put"];
            var deleteOp = swagger["paths"]["/obsoleteactions/{id}"]["delete"];

            Assert.IsNotNull(putOp);
            Assert.IsNull(deleteOp);
        }

        [Test]
        public void It_exposes_config_to_customize_the_grouping_of_actions()
        {
            SetUpDefaultRouteFor<ProductsController>();
            SetUpHandler(c => c.GroupActionsBy(apiDesc => apiDesc.HttpMethod.ToString()));

            var swagger = GetContent<JObject>("http://tempuri.org/swagger/docs/v1");
            foreach (var method in new[] { "post", "get" })
            {
                var tags = swagger["paths"]["/products"][method]["tags"];
                CollectionAssert.IsNotEmpty(tags);
                Assert.AreEqual(method.ToUpper(), tags.First().ToString());
            }
        }

        [Test]
        public void It_exposes_config_to_customize_the_ordering_of_action_groups()
        {
            SetUpDefaultRoutesFor(new[] { typeof(ProductsController), typeof(CustomersController) });
            SetUpHandler(c => c.OrderActionGroupsBy(new DescendingAlphabeticComparer()));

            var swagger = GetContent<JObject>("http://tempuri.org/swagger/docs/v1");
            var paths = swagger["paths"].Value<JObject>()
                .Properties()
                .Select(prop => prop.Name);

            CollectionAssert.AreEqual(new[] { "/products", "/products/{id}", "/customers", "/customers/{id}" }, paths);
       }

        [Test]
        public void It_exposes_config_to_post_modify_the_document()
        {
            SetUpHandler(c => c.DocumentFilter<ApplyDocumentVendorExtensions>());

            var swagger = GetContent<JObject>("http://tempuri.org/swagger/docs/v1");
            var xProp = swagger["x-document"];

            Assert.IsNotNull(xProp);
            Assert.AreEqual("foo", xProp.ToString());
        }

        [Test]
        public void It_exposes_config_to_post_modify_operations()
        {
            SetUpDefaultRouteFor<ProductsController>();
            SetUpHandler(c => c.OperationFilter<AddDefaultResponse>());

            var swagger = GetContent<JObject>("http://tempuri.org/swagger/docs/v1");
            var getDefaultResponse = swagger["paths"]["/products"]["get"]["responses"]["default"];
            var postDefaultResponse = swagger["paths"]["/products"]["post"]["responses"]["default"];

            Assert.IsNotNull(getDefaultResponse);
            Assert.IsNotNull(postDefaultResponse);
        }

        [Test]
        public void It_exposes_config_to_describe_multiple_api_versions()
        {
            SetUpAttributeRoutesFrom(typeof(MultipleApiVersionsController).Assembly);
            SetUpHandler(c =>
                {
                    c.MultipleApiVersions(
                        (apiDesc, targetApiVersion) => SwaggerConfig.ResolveVersionSupportByRouteConstraint(apiDesc, targetApiVersion),
                        (vc) =>
                        {
                            vc.Version("v2", "Test API V2");
                            vc.Version("v1", "Test API V1");
                        });
                });
            
            // 2.0
            var swagger = GetContent<JObject>("http://tempuri.org/swagger/docs/v2");
            var info = swagger["info"];
            var expected = JObject.FromObject(new
                {
                    version = "v2",
                    title = "Test API V2",
                });
            Assert.AreEqual(expected.ToString(), info.ToString());
            Assert.IsNotNull(swagger["paths"]["/{apiVersion}/todos"]);
            Assert.IsNotNull(swagger["paths"]["/{apiVersion}/todos/{id}"]);
            
            // 1.0
            swagger = GetContent<JObject>("http://tempuri.org/swagger/docs/v1");
            info = swagger["info"];
            expected = JObject.FromObject(new
                {
                    version = "v1",
                    title = "Test API V1",
                });
            Assert.AreEqual(expected.ToString(), info.ToString());
            Assert.IsNotNull(swagger["paths"]["/{apiVersion}/todos"]);
            Assert.IsNull(swagger["paths"]["/{apiVersion}/todos/{id}"]);
        }

        [Test]
        public void It_exposes_config_to_workaround_multiple_actions_with_same_path_and_method()
        {
            SetUpDefaultRouteFor<ConflictingActionsController>();
            SetUpHandler(c => c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First()));

            var swagger = GetContent<JObject>("http://tempuri.org/swagger/docs/v1");
            var operations = swagger["paths"]["/conflictingactions"];

            Assert.AreEqual(1, operations.Count());
        }

        [Test]
        public void It_handles_additional_route_parameters()
        {
            // i.e. route params that are not included in the action signature
            SetUpCustomRouteFor<ProductsController>("{apiVersion}/products");

            var swagger = GetContent<JObject>("http://tempuri.org/swagger/docs/v1");
            var getParams = swagger["paths"]["/{apiVersion}/products"]["get"]["parameters"];

            var expected = JArray.FromObject(new object[]
                {
                    new
                    {
                        name = "type",
                        @in = "query",
                        required = true,
                        type = "integer",
                        format = "int32",
                        @enum = new[] { 2, 4 }
                    },
                    new
                    {
                        name = "apiVersion",
                        @in = "path",
                        required = true,
                        type = "string",
                    }
                });

            Assert.AreEqual(expected.ToString(), getParams.ToString());
        }

        [Test]
        public void It_handles_attribute_routes()
        {
            SetUpAttributeRoutesFrom(typeof(AttributeRoutesController).Assembly);

            var swagger = GetContent<JObject>("http://tempuri.org/swagger/docs/v1");
            var path = swagger["paths"]["/subscriptions/{id}/cancel"];
            Assert.IsNotNull(path);
        }

        [Test]
        public void It_handles_json_formatter_not_present()
        {
            SetUpDefaultRouteFor<ProductsController>();
            this.Configuration.Formatters.Clear();

            var response = Get("http://tempuri.org/swagger/docs/v1");
        }

        [Test]
        public void It_errors_on_unknown_api_version_and_returns_status_not_found()
        {
            var response = Get("http://tempuri.org/swagger/docs/1.1");

            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Test]
        public void It_errors_on_multiple_actions_with_same_path_and_method()
        {
            SetUpDefaultRouteFor<ConflictingActionsController>();

            Assert.Throws<NotSupportedException>(
                delegate { GetContent<JObject>("http://tempuri.org/swagger/docs/v1"); });
        }
    }
}