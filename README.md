Swashbuckle
=========
Seamlessly adds a [Swagger](https://developers.helloreverb.com/swagger) to WebApi projects! Uses a combination of ApiExplorer and Swagger/Swagger-UI to provide a rich discovery and documentation experience for consumers.

Getting Started
--------------------

To start exposing auto-generated Swagger docs and a Swagger UI, simply install the Nuget package from your WebApi project:

    Install-Package Swashbuckle

This will add a reference to Swashbuckle.dll which contains an embedded "Area" for Swagger. The Area includes the following enpoints for the raw swagger docs and ui respectively:

swagger/api-docs

swagger

NOTE: The Swagger spec groups endpoints by resource (Resource Listing). When generating the Swagger spec, Swashbuckle creates this grouping by controller name. This affects the way API's are grouped in the UI. For the majority of cases, where the controller-per-resource convention is used, this amounts to the same thing. For other cases, it's worth noting that the grouping may not correspond exactly to the resource. 

Extensibility
--------------------

Swashbuckle automtically generates a Swagger spec and UI based off the WebApi ApiExplorer. The out-of-the-box generator caters for the majority of WebApi implementations but also includes some extensibility points for application-specific needs ...

## swagger-ui customizations

The Swagger UI supports a number of customizations that can be applied by modifying the JavaScript in index.html. Each setting is explained in the [documentation](https://github.com/wordnik/swagger-ui).

In Swashbuckle however, this file is not accessible as it's embedded in the library rather than copied to your application. So, Swashbuckle includes a configuration API that allows you to customize each of these settings on application start up.

    SwaggerUiConfig.Customize(c =>
    {
        c.SupportHeaderParams = true;
        c.DocExpansion = DocExpansion.List;
        c.SupportedSubmitMethods = new[] {HttpMethod.Get, HttpMethod.Post, HttpMethod.Put, HttpMethod.Head, HttpMethod.Delete};
        c.AddOnCompleteScript(typeof(Bootstrapper).Assembly, "YourApp.swagger_ui.ext.after-load.js");
        c.AddStylesheet(typeof(Bootstrapper).Assembly, "YourApp.swagger_ui.ext.stylesheet.css");
    });
       
It's possible to include one or more custom JavaScript files to be executed as soon as the UI is rendered. You can use this to add additional UI components through the DOM. The UI itself depends on jquery and so it can be used in your custom script.

To add a custom script, you need to include it in your project as an Embedded Resource. You can then inject it by calling AddOnCopmpleteScript as shown above. The first parameter is the assembly which contains the Embedded Resource and the second is the Logical Name of that resource. When you add a file through Visual Studio and change it's Build Action property to "Embedded Resource", it will have the following Logical Name:

\<Project Default Namespace>.\<Escaped Folder Path>.\<File Name>

For example, if your app's default namespace is "YourApp", and you want to include a script at the following path within your project - "swagger-ui/ext/after-load.js", then it will, by default, be assigned the following Logical Name at build time:

"YourApp.swagger_ui.ext.after-load.js"

Same approach applies to custom stylesheets. Unlike the injected scripts, which are executed only after the UI is rendered, injected stylesheets appear as <link rel='stylesheet' ... /> elements in the <head> tag, so they are loaded prior to UI rendering. This extension point may be used for altering the appearance of the UI itself in a simpler way.

To add a custom stylesheet, you need to include it as an Embedded Resource, and treat it as described above for JavaScript includes.
        
## Ammend generated operation specs 

There may also be cases where you'd like to make ammendments to the spec after it's been generated. One example would be adding some app specific error codes for each individual operation.

To do this, Swashbuckle provides a separate configuration where you can specify one or more filters to be applied to the generated operation specs:

    SwaggerSpecConfig.Customize(c =>
    {
        c.PostFilter<ApplyHeaderParamsFilter>();
        c.PostFilter<ApplyErrorCodesFilter>();
    });
    
By implementing the IOperationSpecFilter interface, you can write filters that hook into the spec generation process. The code below shows an example that adds error codes based on the AuthorizeAttribute:

    public class ApplyErrorCodesFilter : IOperationSpecFilter
    {
        public void Apply(ApiDescription apiDescription, ApiOperationSpec operationSpec)
        {
            if (apiDescription.ActionDescriptor.GetFilters().OfType<AuthorizeAttribute>().Any())
            {
                operationSpec.errorResponses.Add(new ApiErrorResponseSpec {code = (int) HttpStatusCode.Unauthorized, reason = "Basic Auth required"});
            }
        }
    }

Another example would be using  XML comments to document API calls. Assume we've got a method commented like this:

    /// <summary> Get all foo's for particular bar </summary>
    /// <param name="barId"> bar identifier </param>
    /// <remarks>Returns all three order items we've got here</remarks>
    /// <response code="200">OK</response>
    public List<Foo> GetAll(int barId) {...}
    
One can implement IDocumentationProvider to read info from these comments, and substite the default one for ApiExplorer. While ApiDescription only has one string field available to put all info, one can serialize desirable information to xml once again (or csv, or json), and then implement IOperationSpecFilter to deserialize it and populate the fields needed:

    var descriptionXml = XElement.Parse(apiDescription.Documentation);

    var notes = descriptionXml.Element("remarks");
    if (notes != null)
        operationSpec.notes = notes.Value;

And so on. The above examles are included in a Swashbuckle.TestApp project.