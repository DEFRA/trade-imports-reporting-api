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
            Testing.Endpoints.ClearanceRequests.Intervals(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Intervals(CreateIntervals(from, to, 2)))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings)
            .UseMethodName($"{nameof(WhenSingleRequest_ShouldBeSingleCount)}_intervals");
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
            Testing.Endpoints.ClearanceRequests.Intervals(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Intervals(CreateIntervals(from, to, 2)))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings)
            .UseMethodName($"{nameof(WhenMultipleRequestForSameMrn_ShouldBeSingleSingleUniqueAndTwoTotal)}_intervals");
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
            Testing.Endpoints.ClearanceRequests.Intervals(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Intervals(CreateIntervals(from, to, 2)))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings)
            .UseMethodName(
                $"{nameof(WhenMultipleRequestForSameMrn_AndOneOutsideFromAndTo_ShouldBeSingleCount)}_intervals"
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
            Testing.Endpoints.ClearanceRequests.Intervals(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Intervals(CreateIntervals(from, to, 2)))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings)
            .UseMethodName($"{nameof(WhenMultipleRequestsForDifferentMrn_ShouldBeExpectedBuckets)}_intervals")
            .UseParameters(unit);
    }

    [Fact]
    public async Task WhenCallingFor24Hours_ShouldBeExpectedBuckets()
    {
        var messageSentAt = new DateTime(2025, 9, 3, 0, 0, 0, DateTimeKind.Utc);

        // First bucket
        await SendClearanceRequest(messageSentAt);
        await SendClearanceRequest(messageSentAt.AddMinutes(1));
        // Second bucket
        await SendClearanceRequest(messageSentAt.AddHours(13));
        await SendClearanceRequest(messageSentAt.AddHours(15));
        await SendClearanceRequest(messageSentAt.AddHours(17));
        await SendClearanceRequest(messageSentAt.AddHours(19));
        // Returned in the second request (see below) as would be the next day based on
        // 2025-09-03 00:00:00 to 2025-09-04 00:00:00
        await SendClearanceRequest(messageSentAt.AddHours(24));

        var from = messageSentAt;
        var to = messageSentAt.AddDays(1);

        // First request
        var response = await DefaultClient.GetAsync(
            Testing.Endpoints.ClearanceRequests.Intervals(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Intervals([from.AddHours(12)]))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings);

        from = from.AddDays(1);
        to = to.AddDays(1);

        // Second request
        response = await DefaultClient.GetAsync(
            Testing.Endpoints.ClearanceRequests.Intervals(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Intervals([from.AddHours(12)]))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings)
            .UseMethodName($"{nameof(WhenCallingFor24Hours_ShouldBeExpectedBuckets)}_second");
    }

    [Fact]
    public async Task WhenHandlingUniqueAndTotal_ShouldBeExpectedBuckets()
    {
        var messageSentAt = new DateTime(2025, 9, 3, 0, 0, 0, DateTimeKind.Utc);
        var from = messageSentAt;
        var to = messageSentAt.AddDays(1);

        var mrn1 = Guid.NewGuid().ToString();
        var mrn2 = Guid.NewGuid().ToString();
        var mrn3 = Guid.NewGuid().ToString();

        // Bucket 1 - 2 x mrn1, 1 x mrn3
        var bucket1 = from;
        await SendClearanceRequest(messageSentAt, mrn: mrn1, wait: false);
        await SendClearanceRequest(messageSentAt.AddMinutes(1), mrn: mrn1, wait: false);
        await WaitForRequestMrn(mrn1, count: 2);
        await SendClearanceRequest(messageSentAt.AddMinutes(10), mrn: mrn3);

        // Bucket 2 - 4 x mrn2, 1 x mrn3
        var bucket2 = bucket1.AddHours(12);
        await SendClearanceRequest(messageSentAt.AddHours(13), mrn: mrn2, wait: false);
        await SendClearanceRequest(messageSentAt.AddHours(15), mrn: mrn2, wait: false);
        await SendClearanceRequest(messageSentAt.AddHours(17), mrn: mrn2, wait: false);
        await SendClearanceRequest(messageSentAt.AddHours(19), mrn: mrn2, wait: false);
        await WaitForRequestMrn(mrn2, count: 4);
        await SendClearanceRequest(messageSentAt.AddHours(13).AddMinutes(10), mrn: mrn3);

        // Bucket 3 - empty
        var bucket3 = to.AddHours(-1);

        var response = await DefaultClient.GetAsync(
            Testing.Endpoints.ClearanceRequests.Intervals(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Intervals([bucket2, bucket3]))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings);
    }
}
