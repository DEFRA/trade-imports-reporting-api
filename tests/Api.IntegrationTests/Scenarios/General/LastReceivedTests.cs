using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsReportingApi.Api.Models;

namespace Defra.TradeImportsReportingApi.Api.IntegrationTests.Scenarios.General;

public class LastReceivedTests(SqsTestFixture sqsTestFixture) : ScenarioTestBase(sqsTestFixture)
{
    [Fact]
    public async Task WhenMultipleFinalisationForSameMrn_LatestShouldBeReturned()
    {
        var mrn = Guid.NewGuid().ToString();
        var messageSentAt = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);
        var resourceEvent1 = CreateResourceEvent(
            mrn,
            ResourceEventResourceTypes.CustomsDeclaration,
            new CustomsDeclaration
            {
                Finalisation = new Finalisation
                {
                    ExternalVersion = 1,
                    FinalState = "0",
                    IsManualRelease = false,
                    MessageSentAt = messageSentAt,
                },
            },
            ResourceEventSubResourceTypes.Finalisation
        );
        var resourceEvent2 = CreateResourceEvent(
            mrn,
            ResourceEventResourceTypes.CustomsDeclaration,
            new CustomsDeclaration
            {
                Finalisation = new Finalisation
                {
                    ExternalVersion = 1,
                    FinalState = "0",
                    IsManualRelease = false,
                    MessageSentAt = messageSentAt.AddSeconds(10),
                },
            },
            ResourceEventSubResourceTypes.Finalisation
        );

        await SendMessage(resourceEvent1, CreateMessageAttributes(resourceEvent1));
        await SendMessage(resourceEvent2, CreateMessageAttributes(resourceEvent2));
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
        var resourceEvent1 = CreateResourceEvent(
            mrn,
            ResourceEventResourceTypes.CustomsDeclaration,
            new CustomsDeclarationEntity { ClearanceRequest = new ClearanceRequest { MessageSentAt = messageSentAt } },
            ResourceEventSubResourceTypes.ClearanceRequest
        );
        var resourceEvent2 = CreateResourceEvent(
            mrn,
            ResourceEventResourceTypes.CustomsDeclaration,
            new CustomsDeclarationEntity
            {
                ClearanceRequest = new ClearanceRequest { MessageSentAt = messageSentAt.AddSeconds(10) },
            },
            ResourceEventSubResourceTypes.ClearanceRequest
        );

        await SendMessage(resourceEvent1, CreateMessageAttributes(resourceEvent1));
        await SendMessage(resourceEvent2, CreateMessageAttributes(resourceEvent2));
        await WaitForRequestMrn(mrn, count: 2);

        var client = CreateHttpClient();

        var response = await client.GetAsync(Testing.Endpoints.LastReceived.Get());

        await VerifyJson(await response.Content.ReadAsStringAsync()).UseStrictJson().DontScrubDateTimes();
    }
}
