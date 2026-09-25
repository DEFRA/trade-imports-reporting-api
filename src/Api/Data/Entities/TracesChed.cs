using Defra.TradeImportsReportingApi.Api.Data.Extensions;

namespace Defra.TradeImportsReportingApi.Api.Data.Entities;

[DbCollection("TracesChed")]
public class TracesChed
{
    public required string Id { get; init; }
    public required DateTime ChedCreated { get; init; }
    public required DateTime Timestamp { get; init; }
    public required string ReferenceNumber { get; init; }

    /// <summary>
    /// See NotificationType for values.
    /// </summary>
    public required string NotificationType { get; init; }
}
