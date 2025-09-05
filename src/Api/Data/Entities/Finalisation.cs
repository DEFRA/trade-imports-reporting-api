namespace Defra.TradeImportsReportingApi.Api.Data.Entities;

public class Finalisation
{
    public required string Id { get; init; }
    public required DateTime Timestamp { get; init; }
    public required string Mrn { get; init; }

    /// <summary>
    /// See ReleaseType for values.
    /// </summary>
    public required string ReleaseType { get; init; }
}
