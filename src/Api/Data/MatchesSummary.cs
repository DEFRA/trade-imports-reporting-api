namespace Defra.TradeImportsReportingApi.Api.Data;

public record MatchesSummary(int Match, int NoMatch, int Total)
{
    public static MatchesSummary Empty => new(0, 0, 0);
}
