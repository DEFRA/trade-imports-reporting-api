using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsReportingApi.Api.Data.Entities;
using Defra.TradeImportsReportingApi.Api.Models;
using Defra.TradeImportsReportingApi.Testing;
using MongoDB.Driver;

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
}
