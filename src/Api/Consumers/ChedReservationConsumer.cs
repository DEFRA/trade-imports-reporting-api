using System.Diagnostics.CodeAnalysis;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsReportingApi.Api.Data;
using Defra.TradeImportsReportingApi.Api.Extensions;
using SlimMessageBus;

namespace Defra.TradeImportsReportingApi.Api.Consumers;

[ExcludeFromCodeCoverage]
public class ChedReservationConsumer(
    IConsumerContext context,
    ILogger<ChedReservationConsumer> logger,
    IDbContext dbContext
) : IConsumer<string>
{
    public async Task OnHandle(string received, CancellationToken cancellationToken)
    {
        var resourceEvent = context.DeserializeResourceEvent<ChedReservationEvent>(received);

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
}
