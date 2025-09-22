using Defra.TradeImportsReportingApi.Api.Data.Entities;
using Defra.TradeImportsReportingApi.Testing;
using FluentAssertions;

namespace Defra.TradeImportsReportingApi.Api.IntegrationTests.Scenarios.Matches;

public class DecisionTests(SqsTestFixture sqsTestFixture) : ScenarioTestBase(sqsTestFixture)
{
    [Theory]
    [InlineData(DecisionCode.NoMatch)]
    [InlineData(DecisionCode.Match)]
    public async Task WhenSingleDecision_ShouldBeSingleCount(string decisionCode)
    {
        var mrnCreated = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);

        await SendDecision(mrnCreated, mrnCreated.AddSeconds(20), decisionCode: decisionCode);

        var from = mrnCreated.AddHours(-1);
        var to = mrnCreated.AddHours(1);
        var response = await DefaultClient.GetAsync(
            Testing.Endpoints.Matches.Summary(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings).UseParameters(decisionCode);

        response = await DefaultClient.GetAsync(
            Testing.Endpoints.Matches.Buckets(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Unit(Units.Hour))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings)
            .UseMethodName($"{nameof(WhenSingleDecision_ShouldBeSingleCount)}_buckets")
            .UseParameters(decisionCode);

        response = await DefaultClient.GetAsync(
            Testing.Endpoints.Matches.Data(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Match(true)) // One test case will return data, the other will not
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings)
            .UseMethodName($"{nameof(WhenSingleDecision_ShouldBeSingleCount)}_data")
            .UseParameters(decisionCode);
    }

    [Fact]
    public async Task WhenMultipleDecisionForSameMrn_ShouldBeSingleCount()
    {
        var mrn = Guid.NewGuid().ToString();
        var mrnCreated = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);
        var expectedTimestamp = mrnCreated.AddSeconds(40);

        await SendDecision(mrnCreated, mrnCreated.AddSeconds(20), mrn, wait: false);
        // Different timestamp
        await SendDecision(mrnCreated, expectedTimestamp, mrn, wait: false);
        await WaitForDecisionMrn(mrn, count: 2);

        var from = mrnCreated.AddHours(-1);
        var to = mrnCreated.AddHours(1);
        var response = await DefaultClient.GetAsync(
            Testing.Endpoints.Matches.Summary(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings);

        response = await DefaultClient.GetAsync(
            Testing.Endpoints.Matches.Buckets(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Unit(Units.Hour))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings)
            .UseMethodName($"{nameof(WhenMultipleDecisionForSameMrn_ShouldBeSingleCount)}_buckets");

        response = await DefaultClient.GetAsync(
            Testing.Endpoints.Matches.Data(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Match(false))
            )
        );

        var verifyResult = await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings)
            .UseMethodName($"{nameof(WhenMultipleDecisionForSameMrn_ShouldBeSingleCount)}_data");

        verifyResult.Text.Should().Contain(expectedTimestamp.ToString("yyyy-MM-ddTHH:mm:ssZ"));
    }

    [Fact]
    public async Task WhenMultipleDecisionForSameMrn_AndChangeFromNoMatchToMatch_ShouldBeSingleCount()
    {
        var mrn = Guid.NewGuid().ToString();
        var mrnCreated = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);
        var expectedTimestamp = mrnCreated.AddSeconds(40);

        await SendDecision(mrnCreated, mrnCreated.AddSeconds(20), mrn, wait: false);
        // Different timestamp and change from NoMatch to Match
        await SendDecision(mrnCreated, expectedTimestamp, mrn, decisionCode: DecisionCode.Match, wait: false);
        await WaitForDecisionMrn(mrn, count: 2);

        var from = mrnCreated.AddHours(-1);
        var to = mrnCreated.AddHours(1);
        var response = await DefaultClient.GetAsync(
            Testing.Endpoints.Matches.Summary(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings);

        response = await DefaultClient.GetAsync(
            Testing.Endpoints.Matches.Buckets(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Unit(Units.Hour))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings)
            .UseMethodName(
                $"{nameof(WhenMultipleDecisionForSameMrn_AndChangeFromNoMatchToMatch_ShouldBeSingleCount)}_buckets"
            );

        response = await DefaultClient.GetAsync(
            Testing.Endpoints.Matches.Data(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Match(true))
            )
        );

        var verifyResult = await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings)
            .UseMethodName(
                $"{nameof(WhenMultipleDecisionForSameMrn_AndChangeFromNoMatchToMatch_ShouldBeSingleCount)}_data"
            );

        verifyResult.Text.Should().Contain(expectedTimestamp.ToString("yyyy-MM-ddTHH:mm:ssZ"));
    }

    [Fact]
    public async Task WhenMultipleDecisionForDifferentMrn_AndOneOutsideFromAndTo_ShouldBeSingleCount()
    {
        var mrnCreated = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);

        await SendDecision(mrnCreated, mrnCreated.AddSeconds(20));
        // Outside From and To
        await SendDecision(mrnCreated.AddHours(2), mrnCreated.AddHours(2).AddSeconds(40));

        var from = mrnCreated.AddHours(-1);
        var to = mrnCreated.AddHours(1);
        var response = await DefaultClient.GetAsync(
            Testing.Endpoints.Matches.Summary(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings);

        response = await DefaultClient.GetAsync(
            Testing.Endpoints.Matches.Buckets(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Unit(Units.Hour))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings)
            .UseMethodName(
                $"{nameof(WhenMultipleDecisionForDifferentMrn_AndOneOutsideFromAndTo_ShouldBeSingleCount)}_buckets"
            );

        response = await DefaultClient.GetAsync(
            Testing.Endpoints.Matches.Data(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Match(false))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings)
            .UseMethodName(
                $"{nameof(WhenMultipleDecisionForDifferentMrn_AndOneOutsideFromAndTo_ShouldBeSingleCount)}_data"
            );
    }

    [Theory]
    [InlineData(Units.Hour)]
    [InlineData(Units.Day)]
    public async Task WhenMultipleDecisionForDifferentMrn_ShouldBeExpectedBuckets(string unit)
    {
        var mrnCreated = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);

        await SendDecision(mrnCreated, mrnCreated.AddSeconds(20));
        await SendDecision(mrnCreated.AddHours(2), mrnCreated.AddHours(2).AddSeconds(40));

        var from = mrnCreated.AddHours(-1);
        var to = mrnCreated.AddHours(3);
        var response = await DefaultClient.GetAsync(
            Testing.Endpoints.Matches.Summary(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings).UseParameters(unit);

        response = await DefaultClient.GetAsync(
            Testing.Endpoints.Matches.Buckets(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Unit(unit))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync(), JsonVerifySettings)
            .UseMethodName($"{nameof(WhenMultipleDecisionForDifferentMrn_ShouldBeExpectedBuckets)}_buckets")
            .UseParameters(unit);
    }
}
