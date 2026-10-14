using System.Diagnostics.CodeAnalysis;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsReportingApi.Api.Data;
using Defra.TradeImportsReportingApi.Api.Extensions;
using SlimMessageBus;

namespace Defra.TradeImportsReportingApi.Api.Consumers;

[ExcludeFromCodeCoverage]
public class TracesChedConsumer(IConsumerContext context, ILogger<TracesChedConsumer> logger, IDbContext dbContext)
    : IConsumer<string>
{
    public async Task OnHandle(string received, CancellationToken cancellationToken)
    {
        var ched = context.DeserializeResourceEvent<TracesChedEvent>(received);
        if (ched.Resource is null)
            throw new InvalidOperationException("Resource is null");

        var incomingChed = ched.Resource.Ched;
        var entityChed = incomingChed.ToTracesChed(
            ched.ResourceId,
            incomingChed.ExchangedDocument.IssueDateTime!.Value.UtcDateTime,
            incomingChed.LastUpdated!.Value.UtcDateTime
        );

        if (entityChed.ShouldBeStored())
        {
            await dbContext.TracesCheds.InsertOneAsync(entityChed, cancellationToken: cancellationToken);
            return;
        }

        logger.LogInformation("Traces CHED ignored {NotificationType}", entityChed.NotificationType);
    }
}
