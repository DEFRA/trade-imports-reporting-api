using Defra.TradeImportsReportingApi.Api.Data.Extensions;

namespace Defra.TradeImportsReportingApi.Api.Data.Entities;

/// <summary>
/// Snapshot of a TRACES CHED, stored with the same shape and values as <see cref="ImportPreNotification"/>.
/// </summary>
[DbCollection("TracesChed")]
public class TracesChed
{
    public required string Id { get; init; }
    public required DateTime Created { get; init; }
    public required DateTime Updated { get; init; }

    /// <summary>
    /// See NotificationType for values.
    /// </summary>
    public required string NotificationType { get; init; }

    /// <summary>
    /// See ImportNotificationStatus for values.
    /// </summary>
    public string? Status { get; set; }
    public ImportPreNotificationCommodity[] Commodities { get; set; } = [];
}
