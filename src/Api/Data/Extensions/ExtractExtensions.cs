using Defra.TradeImportsReportingApi.Api.Data.Entities;
using Defra.TradeImportsReportingApi.Api.Endpoints.Dtos;

namespace Defra.TradeImportsReportingApi.Api.Data.Extensions;

public static class ExtractExtensions
{
    private const string Separator = "; ";

    public static IEnumerable<ChedExtractResponse> ToExtractRows(this ImportPreNotification notification) =>
        ToChedExtractRows(
            notification.Id,
            notification.NotificationType,
            notification.Status,
            notification.Created,
            notification.Updated,
            notification.Commodities
        );

    public static IEnumerable<ChedExtractResponse> ToExtractRows(this TracesChed ched) =>
        ToChedExtractRows(ched.Id, ched.NotificationType, ched.Status, ched.Created, ched.Updated, ched.Commodities);

    // Items on the snapshot are per commodity, check and decision result, so are grouped back to one row per commodity
    public static IEnumerable<CustomsDeclarationExtractResponse> ToExtractRows(
        this CustomsDeclaration customsDeclaration
    ) =>
        customsDeclaration
            .Items.GroupBy(x => x.Number)
            .OrderBy(x => x.Key)
            .Select(items =>
            {
                var first = items.First();

                return new CustomsDeclarationExtractResponse
                {
                    Mrn = customsDeclaration.Id,
                    MrnCreated = customsDeclaration.MrnCreated,
                    LastUpdated = customsDeclaration.LastUpdated,
                    ReleaseType = customsDeclaration.ReleaseType,
                    FinalState = customsDeclaration.FinalState,
                    DispatchCountryCode = customsDeclaration.DispatchCountryCode,
                    ItemNumber = items.Key,
                    CommodityCode = first.CommodityCode,
                    Description = first.Description,
                    QuantityOrWeight = first.QuantityOrWeight,
                    ChedReferences = JoinDistinct(items.Select(x => x.ChedReference)),
                    Authorities = JoinDistinct(items.Select(x => x.Authority)),
                    CheckCodes = JoinDistinct(items.Select(x => x.CheckCode)),
                    Decisions = JoinDistinct(items.Select(x => x.Decision)),
                    Match = items.All(x => x.Match == true) ? "Yes" : "No",
                };
            });

    private static IEnumerable<ChedExtractResponse> ToChedExtractRows(
        string chedReference,
        string chedType,
        string? status,
        DateTime created,
        DateTime updated,
        IEnumerable<ImportPreNotificationCommodity> commodities
    ) =>
        commodities.Select(commodity => new ChedExtractResponse
        {
            ChedReference = chedReference,
            ChedType = chedType,
            Status = status,
            Created = created,
            Updated = updated,
            CommodityCode = commodity.CommodityCode,
            Description = commodity.Description,
        });

    private static string? JoinDistinct(IEnumerable<string?> values)
    {
        var distinct = values.Where(x => !string.IsNullOrEmpty(x)).Distinct().ToArray();

        return distinct.Length > 0 ? string.Join(Separator, distinct) : null;
    }
}
