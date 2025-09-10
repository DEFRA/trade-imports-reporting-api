using System.Diagnostics.CodeAnalysis;

namespace Defra.TradeImportsReportingApi.Api.OpenApi;

[ExcludeFromCodeCoverage]
public static class WebApplicationExtensions
{
    public static void UseOpenApi(this WebApplication app)
    {
        app.UseSwagger(options =>
        {
            options.RouteTemplate = "/.well-known/openapi/{documentName}/openapi.json";
        });
        app.UseReDoc(options =>
        {
            options.ConfigObject.ExpandResponses = "200";
            options.DocumentTitle = "Trade Import Reporting API";
            options.RoutePrefix = "redoc";
            options.SpecUrl = "/.well-known/openapi/v1/openapi.json";
        });
    }
}
