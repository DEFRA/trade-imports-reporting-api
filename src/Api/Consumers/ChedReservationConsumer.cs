using System.Diagnostics.CodeAnalysis;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsReportingApi.Api.Data;
using Defra.TradeImportsReportingApi.Api.Extensions;
using SlimMessageBus;

namespace Defra.TradeImportsReportingApi.Api.Consumers;

[ExcludeFromCodeCoverage]
public class ChedReservationConsumer(IConsumerContext context, IDbContext dbContext) : IConsumer<string>
{
    public async Task OnHandle(string received, CancellationToken cancellationToken)
    {
        var resourceEvent = context.DeserializeResourceEvent<ChedReservationEvent>(received);

        if (resourceEvent.Operation is not ResourceEventOperations.Deleted && resourceEvent.Resource is null)
            throw new InvalidOperationException("Resource is null");

        await dbContext.ChedReservations.InsertOneAsync(
            resourceEvent.ToChedReservation(),
            cancellationToken: cancellationToken
        );
    }
}
