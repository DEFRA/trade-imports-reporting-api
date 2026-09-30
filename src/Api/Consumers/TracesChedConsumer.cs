using System.Diagnostics.CodeAnalysis;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsReportingApi.Api.Data;
using Defra.TradeImportsReportingApi.Api.Extensions;
using Defra.TradeImportsReportingApi.Api.Utils;
using SlimMessageBus;
using Trade.Gateway.Api.Contract.Certificate;

namespace Defra.TradeImportsReportingApi.Api.Consumers;

[ExcludeFromCodeCoverage]
public class TracesChedConsumer(IConsumerContext context, ILogger<TracesChedConsumer> logger, IDbContext dbContext)
    : IConsumer<string>
{
    public async Task OnHandle(string received, CancellationToken cancellationToken)
    {
        var ched = DeserializeReceived<DefraUNVTDCHEDProfile>(received);
        if (ched.Resource is null)
            throw new InvalidOperationException("Resource is null");

        var incomingChed = ched.Resource;
        var entityChed = incomingChed.ToTracesChed(
            ched.ResourceId,
            ched.Resource.ExchangedDocument.IssueDateTime!.Value.UtcDateTime,
            ched.Resource.LastUpdated!.Value.UtcDateTime
        );

        if (entityChed.ShouldBeStored())
        {
            await dbContext.TracesCheds.InsertOneAsync(entityChed, cancellationToken: cancellationToken);
            return;
        }

        logger.LogInformation("Notification ignored {ImportNotificationType}", entityChed.NotificationType);
    }

    private ResourceEvent<T> DeserializeReceived<T>(string received) =>
        MessageDeserializer.Deserialize<ResourceEvent<T>>(received, context.Headers.GetContentEncoding())
        ?? throw new InvalidOperationException("Failed to deserialize message");
}
