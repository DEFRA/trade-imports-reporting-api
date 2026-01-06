namespace Defra.TradeImportsReportingApi.Api.IntegrationTests.Scenarios.General;

public class LastSentTests(SqsTestFixture sqsTestFixture) : ScenarioTestBase(sqsTestFixture)
{
    [Fact]
    public async Task WhenMultipleDecisionForSameMrn__AndNoActivity_ThenFallbackLatestShouldBeReturned()
    {
        var mrn = Guid.NewGuid().ToString();
        var mrnCreated = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);

        await SendDecision(mrnCreated, mrnCreated.AddSeconds(1), mrn, wait: false);
        await SendDecision(mrnCreated, mrnCreated.AddSeconds(10), mrn, wait: false);
        await WaitForDecisionMrn(mrn, count: 2);

        var response = await DefaultClient.GetAsync(Testing.Endpoints.LastSent.Get());

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings);
    }

    [Fact]
    public async Task WhenMultipleDecisionForSameMrn__AndActivityExists_ThenFallbackShouldNotBeReturned()
    {
        var mrn = Guid.NewGuid().ToString();
        var mrnCreated = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);

        await SendBtmsToCdsActivity(mrnCreated, mrn, wait: true);

        var response = await DefaultClient.GetAsync(Testing.Endpoints.LastSent.Get());

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings);
    }
}
