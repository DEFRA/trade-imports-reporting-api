using Defra.TradeImportsReportingApi.Api.Data.Extensions;

namespace Defra.TradeImportsReportingApi.Api.Data.Entities;

[DbCollection("Request")]
public class Request
{
    public required string Id { get; init; }
    public required DateTime Timestamp { get; init; }
    public required string Mrn { get; init; }
}
