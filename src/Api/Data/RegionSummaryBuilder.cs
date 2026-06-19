using Defra.TradeImportsReportingApi.Api.Data.Entities;

namespace Defra.TradeImportsReportingApi.Api.Data;

public static class RegionSummaryBuilder
{
    public static RegionSummary ToRegionSummary(this IEnumerable<CustomsDeclaration> declarations)
    {
        var items = declarations.ToList();

        var matches = items
            .Where(x => x.MatchLevel1 == true || x.MatchLevel2 == true || x.MatchLevel3 == true)
            .ToList();

        var noMatches = items
            .Where(x => x.MatchLevel1 == false || x.MatchLevel2 == false || x.MatchLevel3 == false)
            .ToList();

        return new RegionSummary(
            Total: items.Count,
            Match: new MatchesSummaryByLevel(
                Total: matches.Count,
                Level1: matches.Count(x => x.MatchLevel1 == true),
                Level2: matches.Count(x => x.MatchLevel2 == true),
                Level3: matches.Count(x => x.MatchLevel3 == true)
            ),
            NoMatch: new MatchesSummaryByLevel(
                Total: noMatches.Count,
                Level1: noMatches.Count(x => x.MatchLevel1 == false),
                Level2: noMatches.Count(x => x.MatchLevel2 == false),
                Level3: noMatches.Count(x => x.MatchLevel3 == false)
            )
        );
    }
}
