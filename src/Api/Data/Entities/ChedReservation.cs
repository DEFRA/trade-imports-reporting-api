using Defra.TradeImportsReportingApi.Api.Data.Extensions;

namespace Defra.TradeImportsReportingApi.Api.Data.Entities;

[DbCollection("ChedReservation")]
public class ChedReservation
{
    public required string Id { get; init; }
    public required DateTime Timestamp { get; init; }
    public required string ResourceId { get; init; }
    public required string Operation { get; init; }
    public string? ChedId { get; init; }
    public string? Mrn { get; init; }
    public string? Status { get; init; }
}
