using System.Diagnostics.CodeAnalysis;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsReportingApi.Api.Data;
using Defra.TradeImportsReportingApi.Api.Data.Entities;
using Defra.TradeImportsReportingApi.Api.Data.Extensions;
using Defra.TradeImportsReportingApi.Api.Extensions;
using Defra.TradeImportsReportingApi.Api.Utils;
using MongoDB.Driver;
using SlimMessageBus;

namespace Defra.TradeImportsReportingApi.Api.Consumers;

[ExcludeFromCodeCoverage]
public class TracesChedEventsConsumer(
    IConsumerContext context,
    ILogger<TracesChedEventsConsumer> logger,
    IDbContext dbContext
) : IConsumer<string>
{
    public async Task OnHandle(string received, CancellationToken cancellationToken)
    {
        var resourceType = context.GetResourceType();
        if (resourceType != ResourceEventResourceTypes.TracesChed)
        {
            logger.LogWarning("Unexpected resource type {ResourceType} ignored", resourceType);
            return;
        }

        var tracesChed =
            MessageDeserializer.Deserialize<ResourceEvent<TracesChedEvent>>(
                received,
                context.Headers.GetContentEncoding()
            ) ?? throw new InvalidOperationException("Failed to deserialize message");

        if (tracesChed.Resource is null)
            throw new InvalidOperationException("Resource is null");

        var snapshot = tracesChed.Resource.ToTracesChed(tracesChed.ResourceId);
        await dbContext.TracesCheds.ReplaceOneAsync(
            Builders<TracesChed>.Filter.Eq(x => x.Id, snapshot.Id),
            snapshot,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken: cancellationToken
        );
    }
}
