using Defra.TradeImportsReportingApi.Api.Data.Entities;
using Defra.TradeImportsReportingApi.Api.Models;
using MongoDB.Bson;
using Trade.Gateway.Api.Contract.Certificate;

namespace Defra.TradeImportsReportingApi.Api.Consumers;

public static class TracesChedExtensions
{
    public static TracesChed ToTracesChed(
        this DefraUNVTDCHEDProfile ched,
        string referenceNumber,
        DateTime notificationCreated,
        DateTime notificationUpdated
    )
    {
        return new TracesChed
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Timestamp = notificationUpdated,
            ReferenceNumber = referenceNumber,
            ChedCreated = notificationCreated,
            NotificationType = ched.ExchangedDocument.Identifier.Split(".").FirstOrDefault() switch
            {
                "CHEDA" => NotificationType.ChedA,
                "CHEDP" => NotificationType.ChedP,
                "CHEDPP" => NotificationType.ChedPP,
                "CHEDD" => NotificationType.ChedD,
                _ => NotificationType.Unknown,
            },
        };
    }

    public static bool ShouldBeStored(this TracesChed notification) =>
        notification.NotificationType is not NotificationType.Unknown;
}
