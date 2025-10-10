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

        var response = await DefaultClient.GetAsync(Testing.Endpoints.LastReceived.Get());

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings);
    }

    [Fact]
    public async Task WhenMultipleRequestForSameMrn_LatestShouldBeReturned()
    {
        var mrn = Guid.NewGuid().ToString();
        var messageSentAt = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);

        await SendClearanceRequest(messageSentAt, mrn, wait: false);
        await SendClearanceRequest(messageSentAt.AddSeconds(10), mrn, wait: false);
        await WaitForRequestMrn(mrn, count: 2);

        var response = await DefaultClient.GetAsync(Testing.Endpoints.LastReceived.Get());

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings);
    }

    [Fact]
    public async Task WhenMultipleNotificationForSameChed_LatestShouldBeReturned()
    {
        var ched = Guid.NewGuid().ToString();
        var created = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);

        await SendNotification(created, ched, wait: false);
        await SendNotification(created.AddSeconds(10), ched, wait: false);
        await WaitForNotificationChed(ched, count: 2);

        var response = await DefaultClient.GetAsync(Testing.Endpoints.LastReceived.Get());

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings);
    }
}
