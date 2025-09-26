using Defra.TradeImportsReportingApi.Api.Data.Entities;
using Defra.TradeImportsReportingApi.Testing;
using FluentAssertions;

namespace Defra.TradeImportsReportingApi.Api.IntegrationTests.Scenarios.Releases;

public class FinalisationTests(SqsTestFixture sqsTestFixture) : ScenarioTestBase(sqsTestFixture)
{
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public async Task WhenSingleFinalisation_ShouldBeSingleCount(bool isManualRelease, bool isCancelled)
    {
        var messageSentAt = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);

        await SendFinalisation(messageSentAt, isCancelled: isCancelled, isManualRelease: isManualRelease);

        var from = messageSentAt.AddHours(-1);
        var to = messageSentAt.AddHours(1);
        var response = await DefaultClient.GetAsync(
            Testing.Endpoints.Releases.Summary(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings)
            .UseParameters(isManualRelease, isCancelled);

        response = await DefaultClient.GetAsync(
            Testing.Endpoints.Releases.Buckets(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Unit(Units.Hour))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings)
            .UseMethodName($"{nameof(WhenSingleFinalisation_ShouldBeSingleCount)}_buckets")
            .UseParameters(isManualRelease, isCancelled);

        response = await DefaultClient.GetAsync(
            Testing.Endpoints.Releases.Data(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.ReleaseType(ReleaseType.Manual))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings)
            .UseMethodName($"{nameof(WhenSingleFinalisation_ShouldBeSingleCount)}_data")
            .UseParameters(isManualRelease, isCancelled);

        response = await DefaultClient.GetAsync(
            Testing.Endpoints.Releases.Intervals(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Intervals(CreateIntervals(from, to, 2)))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings)
            .UseMethodName($"{nameof(WhenSingleFinalisation_ShouldBeSingleCount)}_intervals")
            .UseParameters(isManualRelease, isCancelled);
    }

    [Fact]
    public async Task WhenMultipleFinalisationForSameMrn_ShouldBeSingleCount()
    {
        var mrn = Guid.NewGuid().ToString();
        var messageSentAt = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);
        var expectedTimestamp = messageSentAt.AddMinutes(5);

        await SendFinalisation(messageSentAt, mrn, wait: false);
        await SendFinalisation(expectedTimestamp, mrn, wait: false);
        await WaitForFinalisationMrn(mrn, count: 2);

        var from = messageSentAt.AddHours(-1);
        var to = messageSentAt.AddHours(1);
        var response = await DefaultClient.GetAsync(
            Testing.Endpoints.Releases.Summary(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings);

        response = await DefaultClient.GetAsync(
            Testing.Endpoints.Releases.Buckets(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Unit(Units.Hour))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings)
            .UseMethodName($"{nameof(WhenMultipleFinalisationForSameMrn_ShouldBeSingleCount)}_buckets");

        response = await DefaultClient.GetAsync(
            Testing.Endpoints.Releases.Data(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.ReleaseType(ReleaseType.Automatic))
            )
        );

        var verifyResult = await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings)
            .UseMethodName($"{nameof(WhenMultipleFinalisationForSameMrn_ShouldBeSingleCount)}_data");

        verifyResult.Text.Should().Contain(expectedTimestamp.ToString("yyyy-MM-ddTHH:mm:ssZ"));

        response = await DefaultClient.GetAsync(
            Testing.Endpoints.Releases.Intervals(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Intervals(CreateIntervals(from, to, 2)))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings)
            .UseMethodName($"{nameof(WhenMultipleFinalisationForSameMrn_ShouldBeSingleCount)}_intervals");
    }

    [Fact]
    public async Task WhenMultipleFinalisationForSameMrn_AndChangeFromAutomaticToManual_ShouldBeSingleCount()
    {
        var mrn = Guid.NewGuid().ToString();
        var messageSentAt = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);
        var expectedTimestamp = messageSentAt.AddMinutes(5);

        await SendFinalisation(messageSentAt, mrn, wait: false);
        // Change from Automatic to Manual and different message sent at
        await SendFinalisation(expectedTimestamp, mrn, isManualRelease: true, wait: false);
        await WaitForFinalisationMrn(mrn, count: 2);

        var from = messageSentAt.AddHours(-1);
        var to = messageSentAt.AddHours(1);
        var response = await DefaultClient.GetAsync(
            Testing.Endpoints.Releases.Summary(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings);

        response = await DefaultClient.GetAsync(
            Testing.Endpoints.Releases.Buckets(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Unit(Units.Hour))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings)
            .UseMethodName(
                $"{nameof(WhenMultipleFinalisationForSameMrn_AndChangeFromAutomaticToManual_ShouldBeSingleCount)}_buckets"
            );

        response = await DefaultClient.GetAsync(
            Testing.Endpoints.Releases.Data(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.ReleaseType(ReleaseType.Manual))
            )
        );

        var verifyResult = await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings)
            .UseMethodName(
                $"{nameof(WhenMultipleFinalisationForSameMrn_AndChangeFromAutomaticToManual_ShouldBeSingleCount)}_data"
            );

        verifyResult.Text.Should().Contain(expectedTimestamp.ToString("yyyy-MM-ddTHH:mm:ssZ"));

        response = await DefaultClient.GetAsync(
            Testing.Endpoints.Releases.Intervals(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Intervals(CreateIntervals(from, to, 2)))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings)
            .UseMethodName(
                $"{nameof(WhenMultipleFinalisationForSameMrn_AndChangeFromAutomaticToManual_ShouldBeSingleCount)}_intervals"
            );
    }

    [Fact]
    public async Task WhenMultipleFinalisationForDifferentMrn_AndOneOutsideFromAndTo_ShouldBeSingleCount()
    {
        var messageSentAt = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);

        await SendFinalisation(messageSentAt);
        // Outside From and To
        await SendFinalisation(messageSentAt.AddHours(2));

        var from = messageSentAt.AddHours(-1);
        var to = messageSentAt.AddHours(1);
        var response = await DefaultClient.GetAsync(
            Testing.Endpoints.Releases.Summary(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings);

        response = await DefaultClient.GetAsync(
            Testing.Endpoints.Releases.Buckets(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Unit(Units.Hour))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings)
            .UseMethodName(
                $"{nameof(WhenMultipleFinalisationForDifferentMrn_AndOneOutsideFromAndTo_ShouldBeSingleCount)}_buckets"
            );

        response = await DefaultClient.GetAsync(
            Testing.Endpoints.Releases.Data(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.ReleaseType(ReleaseType.Automatic))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings)
            .UseMethodName(
                $"{nameof(WhenMultipleFinalisationForDifferentMrn_AndOneOutsideFromAndTo_ShouldBeSingleCount)}_data"
            );

        response = await DefaultClient.GetAsync(
            Testing.Endpoints.Releases.Intervals(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Intervals(CreateIntervals(from, to, 2)))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings)
            .UseMethodName(
                $"{nameof(WhenMultipleFinalisationForDifferentMrn_AndOneOutsideFromAndTo_ShouldBeSingleCount)}_intervals"
            );
    }

    [Theory]
    [InlineData(Units.Hour)]
    [InlineData(Units.Day)]
    public async Task WhenMultipleFinalisationForDifferentMrn_ShouldBeExpectedBuckets(string unit)
    {
        var messageSentAt = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);

        await SendFinalisation(messageSentAt);
        await SendFinalisation(messageSentAt.AddHours(2));

        var from = messageSentAt.AddHours(-1);
        var to = messageSentAt.AddHours(3);
        var response = await DefaultClient.GetAsync(
            Testing.Endpoints.Releases.Summary(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings).UseParameters(unit);

        response = await DefaultClient.GetAsync(
            Testing.Endpoints.Releases.Buckets(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Unit(unit))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings)
            .UseMethodName($"{nameof(WhenMultipleFinalisationForDifferentMrn_ShouldBeExpectedBuckets)}_buckets")
            .UseParameters(unit);

        response = await DefaultClient.GetAsync(
            Testing.Endpoints.Releases.Intervals(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Intervals(CreateIntervals(from, to, 2)))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings)
            .UseMethodName($"{nameof(WhenMultipleFinalisationForDifferentMrn_ShouldBeExpectedBuckets)}_intervals")
            .UseParameters(unit);
    }

    [Fact]
    public async Task WhenCallingFor24Hours_ShouldBeExpectedBuckets()
    {
        var messageSentAt = new DateTime(2025, 9, 3, 0, 0, 0, DateTimeKind.Utc);

        // First bucket
        await SendFinalisation(messageSentAt);
        await SendFinalisation(messageSentAt.AddMinutes(1));
        // Second bucket
        await SendFinalisation(messageSentAt.AddHours(13));
        await SendFinalisation(messageSentAt.AddHours(15));
        await SendFinalisation(messageSentAt.AddHours(17));
        await SendFinalisation(messageSentAt.AddHours(19));
        // Returned in the second request (see below) as would be the next day based on
        // 2025-09-03 00:00:00 to 2025-09-04 00:00:00
        await SendFinalisation(messageSentAt.AddHours(24));

        var from = messageSentAt;
        var to = messageSentAt.AddDays(1);

        // First request
        var response = await DefaultClient.GetAsync(
            Testing.Endpoints.Releases.Intervals(
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
            Testing.Endpoints.Releases.Intervals(
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
    public async Task WhenRequestingData_ShouldBeOrdered()
    {
        var messageSentAt = new DateTime(2025, 9, 3, 0, 0, 0, DateTimeKind.Utc);

        await SendFinalisation(messageSentAt.AddMinutes(-10), mrn: "oldest");
        await SendFinalisation(messageSentAt, mrn: "newest");

        var from = messageSentAt.AddDays(-1);
        var to = messageSentAt.AddDays(1);

        var response = await DefaultClient.GetAsync(
            Testing.Endpoints.Releases.Data(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.ReleaseType(ReleaseType.Automatic))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings);
    }
}
