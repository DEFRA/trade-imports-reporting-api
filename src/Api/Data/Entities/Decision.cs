namespace Defra.TradeImportsReportingApi.Api.Data.Entities;

public class Decision
{
    public required string Id { get; init; }
    public required DateTime MrnCreated { get; init; }
    public required DateTime Timestamp { get; init; }
    public required string Mrn { get; init; }
    public bool Match { get; init; }
}
