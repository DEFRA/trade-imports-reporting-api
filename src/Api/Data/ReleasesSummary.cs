namespace Defra.TradeImportsReportingApi.Api.Data;

public record ReleasesSummary(int Automatic, int Manual, int Total)
{
    public static ReleasesSummary Empty => new(0, 0, 0);
}
