using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.OpenApi.Models;

namespace Defra.TradeImportsReportingApi.Api.OpenApi;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static void AddOpenApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.AddServer(
                new OpenApiServer { Url = "https://" + (configuration.GetValue<string>("OpenApi:Host") ?? "localhost") }
            );
            c.AddSecurityDefinition(
                "Basic",
                new OpenApiSecurityScheme
                {
                    Description = "RFC8725 Compliant JWT",
                    In = ParameterLocation.Header,
                    Name = "Authorization",
                    Scheme = "Basic",
                    Type = SecuritySchemeType.Http,
                }
            );
            c.AddSecurityRequirement(
                new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Basic" },
                        },
                        []
                    },
                }
            );
            c.IncludeXmlComments(Assembly.GetExecutingAssembly());
            c.SupportNonNullableReferenceTypes();
            c.UseAllOfToExtendReferenceSchemas();
            c.SwaggerDoc(
                "v1",
                new OpenApiInfo
                {
                    Description = "TBC",
                    Contact = new OpenApiContact
                    {
                        Email = "tbc@defra.gov.uk",
                        Name = "DEFRA",
                        Url = new Uri(
#pragma warning disable S1075
                            "https://www.gov.uk/government/organisations/department-for-environment-food-rural-affairs"
#pragma warning restore S1075
                        ),
                    },
                    Title = "Trade Imports Reporting API",
                    Version = "v1",
                }
            );
        });
    }
}
