using Defra.TradeImportsReportingApi.Api.Data.Entities;
using MongoDB.Bson;

namespace Defra.TradeImportsReportingApi.Api.Consumers;

public static class DecisionExtensions
{
    public static Decision ToDecision(
        this TradeImportsDataApi.Domain.CustomsDeclaration.ClearanceDecision decision,
        string mrn,
        DateTime mrnCreated
    )
    {
        return new Decision
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Timestamp = decision.Created,
            Mrn = mrn,
            MrnCreated = mrnCreated,
            Match = decision.Items.All(x => x.Checks.All(y => y.DecisionCode is not DecisionCode.NoMatch)),
        };
    }
}
