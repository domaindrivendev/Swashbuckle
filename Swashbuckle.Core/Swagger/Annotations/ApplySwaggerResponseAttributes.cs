using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http.Description;

namespace Swashbuckle.Swagger.Annotations
{
    public class ApplySwaggerResponseAttributes : IOperationFilter
    {
        public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
        {
            if (apiDescription.GetControllerAndActionAttributes<SwaggerResponseRemoveDefaultsAttribute>().Any()) 
                operation.responses.Clear();

            var responseAttributes = apiDescription.GetControllerAndActionAttributes<SwaggerResponseAttribute>()
                .OrderBy(attr => attr.StatusCode);

            foreach (var attr in responseAttributes)
            {
                var statusCode = attr.StatusCode.ToString();

                operation.responses[statusCode] = new Response
                {
                    description = attr.Description ?? InferDescriptionFrom(statusCode),
                    schema = (attr.Type != null) ? schemaRegistry.GetOrRegister(attr.Type, attr.TypeName) : null
                };
            }
        }

        private string InferDescriptionFrom(string statusCode)
        {
            HttpStatusCode enumValue;
            if (Enum.TryParse(statusCode, true, out enumValue))
            {
                return enumValue.ToString();
            }
            return null;
        }
    }
}