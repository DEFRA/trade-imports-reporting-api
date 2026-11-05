using Defra.TradeImportsReportingApi.Api.Data.Extensions;

namespace Defra.TradeImportsReportingApi.Api.Data.Entities;

[DbCollection("ImportPreNotification")]
public class ImportPreNotification
{
    public required string Id { get; init; }
    public required DateTime Created { get; init; }
    public required DateTime Updated { get; init; }

    /// <summary>
    /// See NotificationType for values.
    /// </summary>
    public required string NotificationType { get; init; }
    public string? Status { get; set; }
    public ImportPreNotificationCommodity[] Commodities { get; set; } = [];
}

public class ImportPreNotificationCommodity
{
    public string? CommodityCode { get; init; }
    public string? Description { get; init; }
}
