namespace Defra.TradeImportsReportingApi.Api.IntegrationTests.Scenarios.General;

public class LastReceivedTests(SqsTestFixture sqsTestFixture) : ScenarioTestBase(sqsTestFixture)
{
    [Fact]
    public async Task WhenMultipleFinalisationForSameMrn_LatestShouldBeReturned()
    {
        var mrn = Guid.NewGuid().ToString();
        var messageSentAt = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);

        await SendFinalisation(messageSentAt, mrn, wait: false);
        await SendFinalisation(messageSentAt.AddSeconds(10), mrn, wait: false);
        await WaitForFinalisationMrn(mrn, count: 2);

        var client = CreateHttpClient();

        var response = await client.GetAsync(Testing.Endpoints.LastReceived.Get());

        await VerifyJson(await response.Content.ReadAsStringAsync()).UseStrictJson().DontScrubDateTimes();
    }

    [Fact]
    public async Task WhenMultipleRequestForSameMrn_LatestShouldBeReturned()
    {
        var mrn = Guid.NewGuid().ToString();
        var messageSentAt = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);

        await SendClearanceRequest(messageSentAt, mrn, wait: false);
        await SendClearanceRequest(messageSentAt.AddSeconds(10), mrn, wait: false);
        await WaitForRequestMrn(mrn, count: 2);

        var client = CreateHttpClient();

        var response = await client.GetAsync(Testing.Endpoints.LastReceived.Get());

        await VerifyJson(await response.Content.ReadAsStringAsync()).UseStrictJson().DontScrubDateTimes();
    }
}
