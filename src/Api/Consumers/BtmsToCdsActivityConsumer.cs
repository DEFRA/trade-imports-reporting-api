using System.Diagnostics.CodeAnalysis;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsReportingApi.Api.Data;
using MongoDB.Driver;
using SlimMessageBus;

namespace Defra.TradeImportsReportingApi.Api.Consumers;

[ExcludeFromCodeCoverage]
public class BtmsToCdsActivityConsumer(IDbContext dbContext) : IConsumer<BtmsActivityEvent<BtmsToCdsActivity>>
{
    private static readonly FilterDefinition<Defra.TradeImportsReportingApi.Api.Data.Entities.BtmsToCdsActivity> s_upsertSentFilter =
        Builders<Defra.TradeImportsReportingApi.Api.Data.Entities.BtmsToCdsActivity>.Filter.Eq(
            r => r.Id,
            "BtmsToCdsActivity_Decision_Sent"
        );

    private static readonly FilterDefinition<Defra.TradeImportsReportingApi.Api.Data.Entities.BtmsToCdsActivity> s_upsertFailedFilter =
        Builders<Defra.TradeImportsReportingApi.Api.Data.Entities.BtmsToCdsActivity>.Filter.Eq(
            r => r.Id,
            "BtmsToCdsActivity_Decision_Failed"
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
            var success = message.Activity.ResponseStatusCode is >= 200 and <= 299;
            var idSuffix = success ? "Sent" : "Failed";
            var entity = new Defra.TradeImportsReportingApi.Api.Data.Entities.BtmsToCdsActivity()
            {
                Id = $"BtmsToCdsActivity_Decision_{idSuffix}",
                Mrn = message.ResourceId,
                Timestamp = message.Timestamp,
                Success = success,
                StatusCode = message.Activity.ResponseStatusCode,
            };

            await dbContext.BtmsToCdsActivities.ReplaceOneAsync(
                success ? s_upsertSentFilter : s_upsertFailedFilter,
                entity,
                s_upsertOptions,
                cancellationToken: cancellationToken
            );
        }
    }
}
