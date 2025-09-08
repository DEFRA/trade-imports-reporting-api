using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDataApi.Domain.Ipaffs;
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
        var utcDate = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);

        var mrn = Guid.NewGuid().ToString();
        var resourceEvent1 = CreateResourceEvent(
            mrn,
            ResourceEventResourceTypes.CustomsDeclaration,
            new CustomsDeclarationEntity { ClearanceRequest = new ClearanceRequest { MessageSentAt = utcDate } },
            ResourceEventSubResourceTypes.ClearanceRequest
        );

        await SendMessage(resourceEvent1, CreateMessageAttributes(resourceEvent1));
        await WaitForRequestMrn(mrn);

        var resourceEvent2 = CreateResourceEvent(
            mrn,
            ResourceEventResourceTypes.CustomsDeclaration,
            new CustomsDeclarationEntity
            {
                Created = utcDate,
                ClearanceDecision = new ClearanceDecision
                {
                    Created = utcDate.AddSeconds(20),
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
                    MessageSentAt = utcDate,
                },
            },
            ResourceEventSubResourceTypes.Finalisation
        );

        await SendMessage(resourceEvent3, CreateMessageAttributes(resourceEvent3));
        await WaitForFinalisationMrn(mrn);

        var ched = Guid.NewGuid().ToString();
        var resourceEvent4 = CreateResourceEvent(
            ched,
            ResourceEventResourceTypes.ImportPreNotification,
            new ImportPreNotificationEntity
            {
                Created = utcDate,
                Updated = utcDate.AddMinutes(1),
                ImportPreNotification = new ImportPreNotification
                {
                    ImportNotificationType = ImportPreNotificationType.CVEDA,
                },
            }
        );

        await SendMessage(resourceEvent4, CreateMessageAttributes(resourceEvent4));
        await WaitForNotificationChed(ched);

        var client = CreateHttpClient();

        var from = utcDate.AddHours(-1);
        var to = utcDate.AddHours(1);
        var response = await client.GetAsync(
            Testing.Endpoints.Summary.Get(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync()).UseStrictJson().DontScrubDateTimes();
    }
}
