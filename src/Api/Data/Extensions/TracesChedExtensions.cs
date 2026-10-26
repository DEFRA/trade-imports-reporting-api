using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsReportingApi.Api.Data.Entities;
using Defra.TradeImportsReportingApi.Api.Models;
using Trade.Gateway.Api.Contract.Certificate;

namespace Defra.TradeImportsReportingApi.Api.Data.Extensions;

public static class TracesChedExtensions
{
    public static TracesChed ToTracesChed(this TracesChedEvent tracesChedEvent, string referenceNumber)
    {
        var ched = tracesChedEvent.Ched;

        return new TracesChed
        {
            Id = referenceNumber,
            Created = tracesChedEvent.Created,
            Updated = tracesChedEvent.Updated,
            NotificationType = ToNotificationType(ched.ExchangedDocument.Identifier),
            Status = ToImportNotificationStatus(ched.ExchangedDocument.DocumentStatusCode),
            Commodities = ToCommodities(ched).ToArray(),
        };
    }

    // TRACES identifiers are prefixed with the CHED type, e.g. CHEDA.GB.2025.1234567
    private static string ToNotificationType(string identifier) =>
        identifier.Split('.')[0] switch
        {
            TracesChedType.CHEDA => NotificationType.ChedA,
            TracesChedType.CHEDP => NotificationType.ChedP,
            TracesChedType.CHEDPP => NotificationType.ChedPP,
            TracesChedType.CHEDD => NotificationType.ChedD,
            _ => NotificationType.Unknown,
        };

    // Unrecognised TRACES codes are kept as received so they are not lost
    private static string? ToImportNotificationStatus(string? documentStatusCode) =>
        documentStatusCode switch
        {
            TracesNotificationStatus.Submitted => ImportNotificationStatus.Submitted,
            TracesNotificationStatus.Validated => ImportNotificationStatus.Validated,
            TracesNotificationStatus.InProgress => ImportNotificationStatus.InProgress,
            TracesNotificationStatus.Replaced => ImportNotificationStatus.Replaced,
            TracesNotificationStatus.Cancelled => ImportNotificationStatus.Cancelled,
            TracesNotificationStatus.Deleted => ImportNotificationStatus.Deleted,
            TracesNotificationStatus.SplitConsignment => ImportNotificationStatus.SplitConsignment,
            _ => documentStatusCode,
        };

    private static IEnumerable<ImportPreNotificationCommodity> ToCommodities(DefraUNVTDCHEDProfile ched)
    {
        var tradeLineItems =
            ched.SpecifiedConsignment?.IncludedConsignmentItem?.SelectMany(item => item.IncludedTradeLineItem ?? [])
            ?? [];

        foreach (var tradeLineItem in tradeLineItems)
        {
            var classification = tradeLineItem.ApplicableClassification?.FirstOrDefault(x =>
                !string.IsNullOrEmpty(x.ClassCode?.Value)
            );

            yield return new ImportPreNotificationCommodity
            {
                CommodityCode = classification?.ClassCode?.Value,
                Description = classification?.ClassName?.FirstOrDefault(),
            };
        }
    }
}
