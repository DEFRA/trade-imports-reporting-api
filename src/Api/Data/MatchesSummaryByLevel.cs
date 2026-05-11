namespace Defra.TradeImportsReportingApi.Api.Data;

public record MatchesSummaryByLevel(int Total, int Level1, int Level2, int Level3)
{
    public static MatchesSummaryByLevel Empty => new(0, 0, 0, 0);
}
