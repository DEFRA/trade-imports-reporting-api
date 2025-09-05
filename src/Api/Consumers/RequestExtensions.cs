using Defra.TradeImportsReportingApi.Api.Data.Entities;
using MongoDB.Bson;

namespace Defra.TradeImportsReportingApi.Api.Consumers;

public static class RequestExtensions
{
    public static Request ToRequest(
        this TradeImportsDataApi.Domain.CustomsDeclaration.ClearanceRequest request,
        string mrn
    )
    {
        if (request.MessageSentAt.Kind is not DateTimeKind.Utc)
            throw new ArgumentException("MessageSentAt must be UTC");

        return new Request
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Timestamp = request.MessageSentAt,
            Mrn = mrn,
        };
    }
}
