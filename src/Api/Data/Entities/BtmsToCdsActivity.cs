using Defra.TradeImportsReportingApi.Api.Data.Extensions;

namespace Defra.TradeImportsReportingApi.Api.Data.Entities;

[DbCollection("BtmsToCdsActivity")]
public class BtmsToCdsActivity
{
    public required string Id { get; init; }
    public required DateTime Timestamp { get; init; }
    public required string Mrn { get; init; }
    public bool Success { get; init; }
    public int StatusCode { get; init; }
}
