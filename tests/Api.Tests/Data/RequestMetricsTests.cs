using System.Diagnostics.Metrics;
using Defra.TradeImportsReportingApi.Api.Data;
using Defra.TradeImportsReportingApi.Api.Data.Entities;
using Defra.TradeImportsReportingApi.Api.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using WireMock.ResponseBuilders;

namespace Defra.TradeImportsReportingApi.Api.Tests.Data;

public class RegionSummaryBuilderTests
{
    private CustomsDeclaration CreateCustomsDeclaration(bool level1Match, bool level2Match, bool level3Match)
    {
        return new CustomsDeclaration
        {
            MatchLevel1 = level1Match,
            MatchLevel2 = level2Match,
            MatchLevel3 = level3Match,
            Id = Guid.NewGuid().ToString(),
            MrnCreated = DateTime.UtcNow,
            Timestamp = DateTime.UtcNow,
        };
    }

    [Fact]
    public async Task When_building_region_summary_counts_should_be_correct()
    {
        CustomsDeclaration[] customsDeclarations =
        [
            CreateCustomsDeclaration(true, true, true),
            CreateCustomsDeclaration(true, true, false),
            CreateCustomsDeclaration(true, false, false),
            CreateCustomsDeclaration(false, false, false),
        ];

        var regionSummary = customsDeclarations.ToRegionSummary();

        await Verify(regionSummary);
    }
}
