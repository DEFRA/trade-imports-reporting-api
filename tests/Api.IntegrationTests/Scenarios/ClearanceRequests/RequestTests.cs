using Defra.TradeImportsReportingApi.Testing;

namespace Defra.TradeImportsReportingApi.Api.IntegrationTests.Scenarios.ClearanceRequests;

public class RequestTests(SqsTestFixture sqsTestFixture) : ScenarioTestBase(sqsTestFixture)
{
    [Fact]
    public async Task WhenSingleRequest_ShouldBeSingleCount()
    {
        var messageSentAt = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);

        await SendClearanceRequest(messageSentAt);

        var from = messageSentAt.AddHours(-1);
        var to = messageSentAt.AddHours(1);
        var response = await DefaultClient.GetAsync(
            Testing.Endpoints.ClearanceRequests.Summary(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings);

        response = await DefaultClient.GetAsync(
            Testing.Endpoints.ClearanceRequests.Buckets(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Unit(Units.Hour))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings)
            .UseMethodName($"{nameof(WhenSingleRequest_ShouldBeSingleCount)}_buckets");
    }

    [Fact]
    public async Task WhenMultipleRequestForSameMrn_ShouldBeSingleSingleUniqueAndTwoTotal()
    {
        var mrn = Guid.NewGuid().ToString();
        var messageSentAt = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);

        await SendClearanceRequest(messageSentAt, mrn, wait: false);
        // Different message sent at
        await SendClearanceRequest(messageSentAt.AddMinutes(10), mrn, wait: false);
        await WaitForRequestMrn(mrn, count: 2);

        var from = messageSentAt.AddHours(-1);
        var to = messageSentAt.AddHours(1);
        var response = await DefaultClient.GetAsync(
            Testing.Endpoints.ClearanceRequests.Summary(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings);

        response = await DefaultClient.GetAsync(
            Testing.Endpoints.ClearanceRequests.Buckets(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Unit(Units.Hour))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings)
            .UseMethodName($"{nameof(WhenMultipleRequestForSameMrn_ShouldBeSingleSingleUniqueAndTwoTotal)}_buckets");
    }

    [Fact]
    public async Task WhenMultipleRequestForSameMrn_AndOneOutsideFromAndTo_ShouldBeSingleCount()
    {
        var mrn = Guid.NewGuid().ToString();
        var messageSentAt = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);

        await SendClearanceRequest(messageSentAt, mrn, wait: false);
        // Outside From and To
        await SendClearanceRequest(messageSentAt.AddHours(2), mrn, wait: false);
        await WaitForRequestMrn(mrn, count: 2);

        var from = messageSentAt.AddHours(-1);
        var to = messageSentAt.AddHours(1);
        var response = await DefaultClient.GetAsync(
            Testing.Endpoints.ClearanceRequests.Summary(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings);

        response = await DefaultClient.GetAsync(
            Testing.Endpoints.ClearanceRequests.Buckets(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Unit(Units.Hour))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings)
            .UseMethodName(
                $"{nameof(WhenMultipleRequestForSameMrn_AndOneOutsideFromAndTo_ShouldBeSingleCount)}_buckets"
            );
    }

    [Theory]
    [InlineData(Units.Hour)]
    [InlineData(Units.Day)]
    public async Task WhenMultipleRequestsForDifferentMrn_ShouldBeExpectedBuckets(string unit)
    {
        var messageSentAt = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);

        await SendClearanceRequest(messageSentAt);
        await SendClearanceRequest(messageSentAt.AddHours(2));

        var from = messageSentAt.AddHours(-1);
        var to = messageSentAt.AddHours(3);
        var response = await DefaultClient.GetAsync(
            Testing.Endpoints.ClearanceRequests.Summary(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings).UseParameters(unit);

        response = await DefaultClient.GetAsync(
            Testing.Endpoints.ClearanceRequests.Buckets(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Unit(unit))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings)
            .UseMethodName($"{nameof(WhenMultipleRequestsForDifferentMrn_ShouldBeExpectedBuckets)}_buckets")
            .UseParameters(unit);
    }
}
