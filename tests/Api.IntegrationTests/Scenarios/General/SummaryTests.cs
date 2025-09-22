using Defra.TradeImportsReportingApi.Testing;

namespace Defra.TradeImportsReportingApi.Api.IntegrationTests.Scenarios.General;

public class SummaryTests(SqsTestFixture sqsTestFixture) : ScenarioTestBase(sqsTestFixture)
{
    [Fact]
    public async Task WhenSingleOfEach_ShouldBeAsExpected()
    {
        var utcDate = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);

        var mrn = Guid.NewGuid().ToString();
        await SendClearanceRequest(utcDate, mrn);
        await SendDecision(utcDate, utcDate.AddSeconds(20), mrn);
        await SendFinalisation(utcDate, mrn);

        var ched = Guid.NewGuid().ToString();
        await SendNotification(utcDate, ched, utcDate.AddMinutes(1));

        var from = utcDate.AddHours(-1);
        var to = utcDate.AddHours(1);
        var response = await DefaultClient.GetAsync(
            Testing.Endpoints.Summary.Get(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync()).UseStrictJson().DontScrubDateTimes();
    }
}
