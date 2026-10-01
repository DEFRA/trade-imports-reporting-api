using System.Diagnostics.CodeAnalysis;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsReportingApi.Api.Data;
using Defra.TradeImportsReportingApi.Api.Extensions;
using Defra.TradeImportsReportingApi.Api.Utils;
using SlimMessageBus;

namespace Defra.TradeImportsReportingApi.Api.Consumers;

[ExcludeFromCodeCoverage]
public class ChedReservationConsumer(IConsumerContext context, ILogger<ChedReservationConsumer> logger, IDbContext dbContext)
    : IConsumer<string>
{
    public async Task OnHandle(string received, CancellationToken cancellationToken)
    {
        var resourceEvent = DeserializeReceived(received);

        if (resourceEvent.Operation is not ResourceEventOperations.Deleted && resourceEvent.Resource is null)
        {
            logger.LogError("Missing resource payload for {ResourceEventId}", resourceEvent.ResourceId);
            return;
        }

        await dbContext.ChedReservations.InsertOneAsync(
            resourceEvent.ToChedReservation(),
            cancellationToken: cancellationToken
        );
    }

    private ResourceEvent<ChedReservationEvent> DeserializeReceived(string received) =>
        MessageDeserializer.Deserialize<ResourceEvent<ChedReservationEvent>>(received, context.Headers.GetContentEncoding())
        ?? throw new InvalidOperationException("Failed to deserialize message");
}
