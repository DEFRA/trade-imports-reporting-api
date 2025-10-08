namespace Defra.TradeImportsReportingApi.Api.IntegrationTests.Scenarios.General;

public class StatusTests(SqsTestFixture sqsTestFixture) : ScenarioTestBase(sqsTestFixture)
{
    [Fact]
    public async Task WhenData_ShouldBeReturned()
    {
        var mrn1 = Guid.NewGuid().ToString();
        var messageSentAt1 = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);

        await SendFinalisation(messageSentAt1, mrn1, wait: false);
        await SendFinalisation(messageSentAt1.AddSeconds(10), mrn1, wait: false);
        await WaitForFinalisationMrn(mrn1, count: 2);

        var mrn2 = Guid.NewGuid().ToString();
        var messageSentAt2 = new DateTime(2025, 9, 4, 16, 8, 0, DateTimeKind.Utc);

        await SendClearanceRequest(messageSentAt2, mrn2, wait: false);
        await SendClearanceRequest(messageSentAt2.AddSeconds(10), mrn2, wait: false);
        await WaitForRequestMrn(mrn2, count: 2);

        var ched1 = Guid.NewGuid().ToString();
        var created1 = new DateTime(2025, 9, 5, 16, 8, 0, DateTimeKind.Utc);

        await SendNotification(created1, ched1, wait: false);
        await SendNotification(created1.AddSeconds(10), ched1, wait: false);
        await WaitForNotificationChed(ched1, count: 2);

        var mrn3 = Guid.NewGuid().ToString();
        var mrnCreated1 = new DateTime(2025, 9, 6, 16, 8, 0, DateTimeKind.Utc);

        await SendDecision(mrnCreated1, mrnCreated1.AddSeconds(1), mrn3, wait: false);
        await SendDecision(mrnCreated1, mrnCreated1.AddSeconds(10), mrn3, wait: false);
        await WaitForDecisionMrn(mrn3, count: 2);

        var response = await DefaultClient.GetAsync(Testing.Endpoints.Status.Get());

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings);
    }
}
