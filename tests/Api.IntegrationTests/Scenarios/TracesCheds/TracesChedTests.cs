using Defra.TradeImportsReportingApi.Api.Data.Entities;
using Defra.TradeImportsReportingApi.Testing;
using MongoDB.Driver;

namespace Defra.TradeImportsReportingApi.Api.IntegrationTests.Scenarios.TracesCheds;

public class TracesChedTests(SqsTestFixture sqsTestFixture) : ScenarioTestBase(sqsTestFixture)
{
    private static readonly DateTime s_issued = new(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);

    [Fact]
    public async Task WhenMultipleTracesChedForSameChed_ShouldBeSingleCount()
    {
        var ched = Guid.NewGuid().ToString();

        await SendTracesChed(s_issued, ched, wait: false);
        await SendTracesChed(s_issued, ched, s_issued.AddMinutes(1), wait: false);
        await WaitForTracesChed(ched, count: 2);

        await VerifyJson(await GetSummary(), JsonVerifySettings);
    }

    [Fact]
    public async Task WhenMultipleTracesChedForDifferentChed_AndOneOutsideFromAndTo_ShouldBeExpectedCounts()
    {
        await SendTracesChed(s_issued, type: "CHEDA");
        await SendTracesChed(s_issued, type: "CHEDP");
        await SendTracesChed(s_issued, type: "CHEDPP");
        await SendTracesChed(s_issued, type: "CHEDD");
        await SendTracesChed(s_issued, lastUpdated: s_issued.AddHours(2), type: "CHEDD");

        await VerifyJson(await GetSummary(), JsonVerifySettings);
    }

    [Fact]
    public async Task WhenUnknownTracesChedType_ShouldNotBeStored()
    {
        var unknownChed = Guid.NewGuid().ToString();
        var knownChed = Guid.NewGuid().ToString();

        await SendTracesChed(s_issued, unknownChed, type: "IMP", wait: false);
        await SendTracesChed(s_issued, knownChed);

        Assert.Equal(
            0,
            await TracesCheds.CountDocumentsAsync(Builders<TracesChed>.Filter.Eq(x => x.ReferenceNumber, unknownChed))
        );

        await VerifyJson(await GetSummary(), JsonVerifySettings);
    }

    private static async Task<string> GetSummary()
    {
        var response = await DefaultClient.GetAsync(
            Testing.Endpoints.Summary.Get(
                EndpointQuery
                    .New.Where(EndpointFilter.From(s_issued.AddHours(-1)))
                    .Where(EndpointFilter.To(s_issued.AddHours(1)))
            )
        );

        return await response.Content.ReadAsStringAsync();
    }
}
