using System.Diagnostics.CodeAnalysis;
using Defra.TradeImportsReportingApi.Api.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Defra.TradeImportsReportingApi.Api.Health;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHealth(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddHealthChecks()
            .AddMongoDb(
                provider => provider.GetRequiredService<IMongoDatabase>(),
                timeout: TimeSpan.FromSeconds(10),
                tags: [WebApplicationExtensions.Extended]
            )
            .AddSqs(
                configuration,
                "Resource events",
                sp => sp.GetRequiredService<IOptions<ResourceEventsConsumerOptions>>().Value.QueueName,
                tags: [WebApplicationExtensions.Extended],
                timeout: TimeSpan.FromSeconds(10)
            );

        return services;
    }
}
