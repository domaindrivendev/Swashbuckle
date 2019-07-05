using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Swashbuckle.Dummy.Controllers;
using Swashbuckle.Dummy.SwaggerExtensions;

namespace Swashbuckle.Tests.Swagger
{
    [TestFixture]
    public class AnnotationsTests : SwaggerTestBase
    {
        public AnnotationsTests()
            : base("swagger/docs/{apiVersion}")
        {
        }

        [SetUp]
        public void SetUp()
        {
            SetUpDefaultRouteFor<SwaggerAnnotatedController>();

            // Default set-up
            SetUpHandler();
        }

        [Test]
        public void It_assigns_operation_properties_from_swagger_operation_attribute()
        {
            var swagger = GetContent<JObject>("http://tempuri.org/swagger/docs/v1");

            var putOperation = swagger["paths"]["/swaggerannotated/{id}"]["put"];

            Assert.AreEqual("UpdateMessage", putOperation["operationId"].ToString());
            Assert.AreEqual(JArray.FromObject(new[] { "messages" }).ToString(), putOperation["tags"].ToString());
            Assert.AreEqual(JArray.FromObject(new[] { "ws" }).ToString(), putOperation["schemes"].ToString());
        }

        [Test]
        public void It_exposes_config_to_post_modify_responses()
        {

            SetUpDefaultRouteFor<ProductsController>();
            SetUpHandler(c => c.OperationFilter<ApplyResponseVendorExtensions>());

            var swagger = GetContent<JObject>("http://tempuri.org/swagger/docs/v1");
            var xProp = swagger["paths"]["/products"]["get"]["responses"]["200"]["x-foo"];

            Assert.IsNotNull(xProp);
            Assert.AreEqual("bar", xProp.ToString());            
        }

        [Test]
        public void It_documents_responses_from_swagger_response_attributes()
        {
            var swagger = GetContent<JObject>("http://tempuri.org/swagger/docs/v1");

            var postResponses = swagger["paths"]["/swaggerannotated"]["post"]["responses"];
            var expected = JObject.FromObject(new Dictionary<string, object>()
                {
                    {
                        "201", new
                        {
                            description = "Created",
                            schema = new
                            {
                                format = "int32",
                                type = "integer"
                            }
                        }
                    },
                    {
                        "400", new
                        {
                            description = "Invalid message",
                            schema = new
                            {
                                type = "object",
                                additionalProperties = new
                                {
                                    type = "object"
                                }
                            }
                        }
                    }
                });
            Assert.AreEqual(expected.ToString(), postResponses.ToString());

            var getResponses = swagger[ "paths" ][ "/swaggerannotated" ][ "get" ][ "responses" ];
            expected = JObject.FromObject( new Dictionary<string, object>()
                {
                    {
                        "200", new
                        {
                            description = "OK",
                            schema = new
                            {
                                type = "array",
                                items = JObject.Parse("{ $ref: \"#/definitions/Message\" }"),
                                xml = JObject.Parse( "{ \"name\": \"Messages\", \"wrapped\": true }" )
                            }
                        }
                    },
                    {
                        "400", new
                        {
                            description = "Bad request"
                        }
                    }
                } );
            Assert.AreEqual( expected.ToString(), getResponses.ToString() );
        }

        [Test]
        public void It_supports_per_type_filters_via_swagger_schema_filter_attribute()
        {
            var swagger = GetContent<JObject>("http://tempuri.org/swagger/docs/v1");

            var messageExamples = swagger["definitions"]["Message"]["default"];
            var expected = JObject.FromObject(new
            {
                title = "A message",
                content = "Some content"
            });

            Assert.AreEqual(expected.ToString(), messageExamples.ToString());
        }

        [Test]
        public void It_supports_per_action_filters_via_swagger_operation_filter_attribute()
        {
            var swagger = GetContent<JObject>("http://tempuri.org/swagger/docs/v1");

            var responseExamples = swagger["paths"]["/swaggerannotated/{id}"]["get"]["responses"]["200"]["examples"];
            var expected = JObject.FromObject(new Dictionary<string, object>()
            {
                { "application/json", new { title = "A message", content = "Some content" } }
            });

            Assert.AreEqual(expected.ToString(), responseExamples.ToString());
        }
    }
}
