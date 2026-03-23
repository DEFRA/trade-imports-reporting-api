using System.Diagnostics.CodeAnalysis;
using Microsoft.OpenApi;

namespace Defra.TradeImportsReportingApi.Api.OpenApi;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static void AddOpenApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenApi(options =>
        {
            options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0;
            options.AddDocumentTransformer(
                (document, context, cancellationToken) =>
                {
                    document.Info = new OpenApiInfo
                    {
                        Title = "Trade Imports Reporting API",
                        Description = "TBC",
                        Version = "v1",
                        Contact = new OpenApiContact()
                        {
                            Email = "tbc@defra.gov.uk",
                            Name = "DEFRA",
                            Url = new Uri(
#pragma warning disable S1075
                                "https://www.gov.uk/government/organisations/department-for-environment-food-rural-affairs"
#pragma warning restore S1075
                            ),
                        },
                    };
                    return Task.CompletedTask;
                }
            );
        });
    }
}
