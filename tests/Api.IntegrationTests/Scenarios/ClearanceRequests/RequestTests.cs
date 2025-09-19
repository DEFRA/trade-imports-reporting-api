using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsReportingApi.Api.Models;
using Defra.TradeImportsReportingApi.Testing;

namespace Defra.TradeImportsReportingApi.Api.IntegrationTests.Scenarios.ClearanceRequests;

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
            Testing.Endpoints.ClearanceRequests.Summary(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseStrictJson()
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();

        response = await client.GetAsync(
            Testing.Endpoints.ClearanceRequests.Buckets(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Unit(Units.Hour))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseMethodName($"{nameof(WhenSingleRequest_ShouldBeSingleCount)}_buckets")
            .UseStrictJson()
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();
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
            Testing.Endpoints.ClearanceRequests.Summary(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseStrictJson()
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();

        response = await client.GetAsync(
            Testing.Endpoints.ClearanceRequests.Buckets(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Unit(Units.Hour))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseMethodName($"{nameof(WhenMultipleRequestForSameMrn_ShouldBeSingleSingleUniqueAndTwoTotal)}_buckets")
            .UseStrictJson()
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();
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
            Testing.Endpoints.ClearanceRequests.Summary(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseStrictJson()
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();

        response = await client.GetAsync(
            Testing.Endpoints.ClearanceRequests.Buckets(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Unit(Units.Hour))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseMethodName(
                $"{nameof(WhenMultipleRequestForSameMrn_AndOneOutsideFromAndTo_ShouldBeSingleCount)}_buckets"
            )
            .UseStrictJson()
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();
    }

    [Theory]
    [InlineData(Units.Hour)]
    [InlineData(Units.Day)]
    public async Task WhenMultipleRequestsForDifferentMrn_ShouldBeExpectedBuckets(string unit)
    {
        var mrn1 = Guid.NewGuid().ToString();
        var mrn2 = Guid.NewGuid().ToString();
        var messageSentAt = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);
        var resourceEvent1 = CreateResourceEvent(
            mrn1,
            ResourceEventResourceTypes.CustomsDeclaration,
            new CustomsDeclarationEntity { ClearanceRequest = new ClearanceRequest { MessageSentAt = messageSentAt } },
            ResourceEventSubResourceTypes.ClearanceRequest
        );
        var resourceEvent2 = CreateResourceEvent(
            mrn2,
            ResourceEventResourceTypes.CustomsDeclaration,
            new CustomsDeclarationEntity
            {
                ClearanceRequest = new ClearanceRequest { MessageSentAt = messageSentAt.AddHours(2) },
            },
            ResourceEventSubResourceTypes.ClearanceRequest
        );

        await SendMessage(resourceEvent1, CreateMessageAttributes(resourceEvent1));
        await SendMessage(resourceEvent2, CreateMessageAttributes(resourceEvent2));
        await WaitForRequestMrn(mrn1);
        await WaitForRequestMrn(mrn2);

        var client = CreateHttpClient();

        var from = messageSentAt.AddHours(-1);
        var to = messageSentAt.AddHours(3);
        var response = await client.GetAsync(
            Testing.Endpoints.ClearanceRequests.Summary(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseParameters(unit)
            .UseStrictJson()
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();

        response = await client.GetAsync(
            Testing.Endpoints.ClearanceRequests.Buckets(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Unit(unit))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseMethodName($"{nameof(WhenMultipleRequestsForDifferentMrn_ShouldBeExpectedBuckets)}_buckets")
            .UseParameters(unit)
            .UseStrictJson()
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();
    }
}
