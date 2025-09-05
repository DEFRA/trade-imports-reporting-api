using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsReportingApi.Api.Data.Entities;
using Defra.TradeImportsReportingApi.Api.Models;
using Defra.TradeImportsReportingApi.Testing;
using Finalisation = Defra.TradeImportsDataApi.Domain.CustomsDeclaration.Finalisation;

namespace Defra.TradeImportsReportingApi.Api.IntegrationTests.Scenarios;

public class SummaryTests(SqsTestFixture sqsTestFixture) : ScenarioTestBase(sqsTestFixture)
{
    [Fact]
    public async Task WhenSingleOfEach_ShouldBeAsExpected()
    {
        var mrn = Guid.NewGuid().ToString();
        var messageSentAt = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);
        var resourceEvent1 = CreateResourceEvent(
            mrn,
            ResourceEventResourceTypes.CustomsDeclaration,
            new CustomsDeclarationEntity { ClearanceRequest = new ClearanceRequest { MessageSentAt = messageSentAt } },
            ResourceEventSubResourceTypes.ClearanceRequest
        );

        await SendMessage(resourceEvent1, CreateMessageAttributes(resourceEvent1));
        await WaitForRequestMrn(mrn);

        var mrnCreated = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);
        var resourceEvent2 = CreateResourceEvent(
            mrn,
            ResourceEventResourceTypes.CustomsDeclaration,
            new CustomsDeclarationEntity
            {
                Created = mrnCreated,
                ClearanceDecision = new ClearanceDecision
                {
                    Created = mrnCreated.AddSeconds(20),
                    Items =
                    [
                        new ClearanceDecisionItem
                        {
                            Checks =
                            [
                                new ClearanceDecisionCheck
                                {
                                    CheckCode = "IGNORE",
                                    DecisionCode = DecisionCode.NoMatch,
                                },
                            ],
                        },
                    ],
                },
            },
            ResourceEventSubResourceTypes.ClearanceDecision
        );

        await SendMessage(resourceEvent2, CreateMessageAttributes(resourceEvent2));
        await WaitForDecisionMrn(mrn);

        var resourceEvent3 = CreateResourceEvent(
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

        await SendMessage(resourceEvent3, CreateMessageAttributes(resourceEvent3));
        await WaitForFinalisationMrn(mrn);

        var client = CreateHttpClient();

        var from = mrnCreated.AddHours(-1);
        var to = mrnCreated.AddHours(1);
        var response = await client.GetAsync(
            Testing.Endpoints.Summary.Get(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync()).UseStrictJson().DontScrubDateTimes();
    }
}
