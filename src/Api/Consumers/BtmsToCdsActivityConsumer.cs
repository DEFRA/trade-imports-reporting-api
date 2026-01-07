using System.Diagnostics.CodeAnalysis;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsReportingApi.Api.Data;
using MongoDB.Driver;
using SlimMessageBus;

namespace Defra.TradeImportsReportingApi.Api.Consumers;

[ExcludeFromCodeCoverage]
public class BtmsToCdsActivityConsumer(IDbContext dbContext) : IConsumer<BtmsActivityEvent<BtmsToCdsActivity>>
{
    private static readonly FilterDefinition<Defra.TradeImportsReportingApi.Api.Data.Entities.BtmsToCdsActivity> s_upsertFilter =
        Builders<Defra.TradeImportsReportingApi.Api.Data.Entities.BtmsToCdsActivity>.Filter.Eq(
            r => r.Id,
            "BtmsToCdsActivity_Decision"
        );
    private static readonly ReplaceOptions s_upsertOptions = new() { IsUpsert = true };

    public async Task OnHandle(BtmsActivityEvent<BtmsToCdsActivity> message, CancellationToken cancellationToken)
    {
        if (
            message is
            {
                ResourceType: ResourceEventResourceTypes.CustomsDeclaration,
                SubResourceType: ResourceEventSubResourceTypes.ClearanceDecision
            }
        )
        {
            var entity = new Defra.TradeImportsReportingApi.Api.Data.Entities.BtmsToCdsActivity()
            {
                Id = "BtmsToCdsActivity_Decision",
                Mrn = message.ResourceId,
                Timestamp = message.Activity.Timestamp,
                Success = message.Activity.StatusCode is >= 200 and <= 299,
                StatusCode = message.Activity.StatusCode,
            };

            await dbContext.BtmsToCdsActivities.ReplaceOneAsync(
                s_upsertFilter,
                entity,
                s_upsertOptions,
                cancellationToken: cancellationToken
            );
        }
    }
}
