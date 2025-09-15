namespace Defra.TradeImportsReportingApi.Api.Data;

public record ClearanceRequestsSummary(int Unique, int Total)
{
    public static ClearanceRequestsSummary Empty => new(0, 0);
}
