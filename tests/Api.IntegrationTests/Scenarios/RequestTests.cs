using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsReportingApi.Api.Models;
using Defra.TradeImportsReportingApi.Testing;

namespace Defra.TradeImportsReportingApi.Api.IntegrationTests.Scenarios;

public class RequestTests(SqsTestFixture sqsTestFixture) : ScenarioTestBase(sqsTestFixture)
{
    [Fact]
    public async Task WhenSingleRequest_ShouldBeSingleCount()
    {
        var mrn = Guid.NewGuid().ToString();
        var messageSentAt = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);
        var resourceEvent = CreateResourceEvent(
            mrn,
            ResourceEventResourceTypes.CustomsDeclaration,
            new CustomsDeclarationEntity { ClearanceRequest = new ClearanceRequest { MessageSentAt = messageSentAt } },
            ResourceEventSubResourceTypes.ClearanceRequest
        );

        await SendMessage(resourceEvent, CreateMessageAttributes(resourceEvent));
        await WaitForRequestMrn(mrn);

        var client = CreateHttpClient();

        var from = messageSentAt.AddHours(-1);
        var to = messageSentAt.AddHours(1);
        var response = await client.GetAsync(
            Testing.Endpoints.ClearanceRequestsSummary.Get(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync()).UseStrictJson().DontScrubDateTimes();
    }

    [Fact]
    public async Task WhenMultipleRequestForSameMrn_ShouldBeSingleSingleUniqueAndTwoTotal()
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
                ClearanceRequest = new ClearanceRequest
                {
                    MessageSentAt = messageSentAt.AddMinutes(10), // Different message sent at
                },
            },
            ResourceEventSubResourceTypes.ClearanceRequest
        );

        await SendMessage(resourceEvent1, CreateMessageAttributes(resourceEvent1));
        await SendMessage(resourceEvent2, CreateMessageAttributes(resourceEvent2));
        await WaitForRequestMrn(mrn, count: 2);

        var client = CreateHttpClient();

        var from = messageSentAt.AddHours(-1);
        var to = messageSentAt.AddHours(1);
        var response = await client.GetAsync(
            Testing.Endpoints.ClearanceRequestsSummary.Get(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync()).UseStrictJson().DontScrubDateTimes();
    }

    [Fact]
    public async Task WhenMultipleRequestForSameMrn_AndOneOutsideFromAndTo_ShouldBeSingleCount()
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
                ClearanceRequest = new ClearanceRequest
                {
                    MessageSentAt = messageSentAt.AddHours(2), // Outside From and To,
                },
            },
            ResourceEventSubResourceTypes.ClearanceRequest
        );

        await SendMessage(resourceEvent1, CreateMessageAttributes(resourceEvent1));
        await SendMessage(resourceEvent2, CreateMessageAttributes(resourceEvent2));
        await WaitForRequestMrn(mrn, count: 2);

        var client = CreateHttpClient();

        var from = messageSentAt.AddHours(-1);
        var to = messageSentAt.AddHours(1);
        var response = await client.GetAsync(
            Testing.Endpoints.ClearanceRequestsSummary.Get(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync()).UseStrictJson().DontScrubDateTimes();
    }
}
