using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsReportingApi.Api.Data.Entities;
using Defra.TradeImportsReportingApi.Testing;
using FluentAssertions;
using Finalisation = Defra.TradeImportsDataApi.Domain.CustomsDeclaration.Finalisation;

namespace Defra.TradeImportsReportingApi.Api.IntegrationTests.Scenarios.Releases;

public class FinalisationTests(SqsTestFixture sqsTestFixture) : ScenarioTestBase(sqsTestFixture)
{
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public async Task WhenSingleFinalisation_ShouldBeSingleCount(bool isManualRelease, bool isCancelled)
    {
        var mrn = Guid.NewGuid().ToString();
        var messageSentAt = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);
        var resourceEvent = CreateResourceEvent(
            mrn,
            ResourceEventResourceTypes.CustomsDeclaration,
            new CustomsDeclaration
            {
                Finalisation = new Finalisation
                {
                    ExternalVersion = 1,
                    FinalState = isCancelled ? "1" : "0",
                    IsManualRelease = isManualRelease,
                    MessageSentAt = messageSentAt,
                },
            },
            ResourceEventSubResourceTypes.Finalisation
        );

        await SendMessage(resourceEvent, CreateMessageAttributes(resourceEvent));
        await WaitForFinalisationMrn(mrn);

        var client = CreateHttpClient();

        var from = messageSentAt.AddHours(-1);
        var to = messageSentAt.AddHours(1);
        var response = await client.GetAsync(
            Testing.Endpoints.Releases.Summary(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseParameters(isManualRelease, isCancelled)
            .UseStrictJson()
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();

        response = await client.GetAsync(
            Testing.Endpoints.Releases.Buckets(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Unit(Units.Hour))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseMethodName($"{nameof(WhenSingleFinalisation_ShouldBeSingleCount)}_buckets")
            .UseParameters(isManualRelease, isCancelled)
            .UseStrictJson()
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();

        response = await client.GetAsync(
            Testing.Endpoints.Releases.Data(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.ReleaseType(ReleaseType.Manual))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseMethodName($"{nameof(WhenSingleFinalisation_ShouldBeSingleCount)}_data")
            .UseParameters(isManualRelease, isCancelled)
            .UseStrictJson()
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();
    }

    [Fact]
    public async Task WhenMultipleFinalisationForSameMrn_ShouldBeSingleCount()
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
        var expectedTimestamp = messageSentAt.AddMinutes(5);
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
                    MessageSentAt = expectedTimestamp, // Different message sent at
                },
            },
            ResourceEventSubResourceTypes.Finalisation
        );

        await SendMessage(resourceEvent1, CreateMessageAttributes(resourceEvent1));
        await SendMessage(resourceEvent2, CreateMessageAttributes(resourceEvent2));
        await WaitForFinalisationMrn(mrn, count: 2);

        var client = CreateHttpClient();

        var from = messageSentAt.AddHours(-1);
        var to = messageSentAt.AddHours(1);
        var response = await client.GetAsync(
            Testing.Endpoints.Releases.Summary(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseStrictJson()
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();

        response = await client.GetAsync(
            Testing.Endpoints.Releases.Buckets(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Unit(Units.Hour))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseMethodName($"{nameof(WhenMultipleFinalisationForSameMrn_ShouldBeSingleCount)}_buckets")
            .UseStrictJson()
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();

        response = await client.GetAsync(
            Testing.Endpoints.Releases.Data(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.ReleaseType(ReleaseType.Automatic))
            )
        );

        var verifyResult = await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseMethodName($"{nameof(WhenMultipleFinalisationForSameMrn_ShouldBeSingleCount)}_data")
            .UseStrictJson()
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();

        verifyResult.Text.Should().Contain(expectedTimestamp.ToString("yyyy-MM-ddTHH:mm:ssZ"));
    }

    [Fact]
    public async Task WhenMultipleFinalisationForSameMrn_AndChangeFromAutomaticToManual_ShouldBeSingleCount()
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
        var expectedTimestamp = messageSentAt.AddMinutes(5);
        var resourceEvent2 = CreateResourceEvent(
            mrn,
            ResourceEventResourceTypes.CustomsDeclaration,
            new CustomsDeclaration
            {
                Finalisation = new Finalisation
                {
                    ExternalVersion = 1,
                    FinalState = "0",
                    IsManualRelease = true, // Change from Automatic to Manual
                    MessageSentAt = expectedTimestamp, // Different message sent at
                },
            },
            ResourceEventSubResourceTypes.Finalisation
        );

        await SendMessage(resourceEvent1, CreateMessageAttributes(resourceEvent1));
        await SendMessage(resourceEvent2, CreateMessageAttributes(resourceEvent2));
        await WaitForFinalisationMrn(mrn, count: 2);

        var client = CreateHttpClient();

        var from = messageSentAt.AddHours(-1);
        var to = messageSentAt.AddHours(1);
        var response = await client.GetAsync(
            Testing.Endpoints.Releases.Summary(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseStrictJson()
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();

        response = await client.GetAsync(
            Testing.Endpoints.Releases.Buckets(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Unit(Units.Hour))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseMethodName(
                $"{nameof(WhenMultipleFinalisationForSameMrn_AndChangeFromAutomaticToManual_ShouldBeSingleCount)}_buckets"
            )
            .UseStrictJson()
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();

        response = await client.GetAsync(
            Testing.Endpoints.Releases.Data(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.ReleaseType(ReleaseType.Manual))
            )
        );

        var verifyResult = await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseMethodName(
                $"{nameof(WhenMultipleFinalisationForSameMrn_AndChangeFromAutomaticToManual_ShouldBeSingleCount)}_data"
            )
            .UseStrictJson()
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();

        verifyResult.Text.Should().Contain(expectedTimestamp.ToString("yyyy-MM-ddTHH:mm:ssZ"));
    }

    [Fact]
    public async Task WhenMultipleFinalisationForDifferentMrn_AndOneOutsideFromAndTo_ShouldBeSingleCount()
    {
        var mrn1 = Guid.NewGuid().ToString();
        var mrn2 = Guid.NewGuid().ToString();
        var messageSentAt = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);
        var resourceEvent1 = CreateResourceEvent(
            mrn1,
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
            mrn2,
            ResourceEventResourceTypes.CustomsDeclaration,
            new CustomsDeclaration
            {
                Finalisation = new Finalisation
                {
                    ExternalVersion = 1,
                    FinalState = "0",
                    IsManualRelease = false,
                    MessageSentAt = messageSentAt.AddHours(2), // Outside From and To
                },
            },
            ResourceEventSubResourceTypes.Finalisation
        );

        await SendMessage(resourceEvent1, CreateMessageAttributes(resourceEvent1));
        await SendMessage(resourceEvent2, CreateMessageAttributes(resourceEvent2));
        await WaitForFinalisationMrn(mrn1);
        await WaitForFinalisationMrn(mrn2);

        var client = CreateHttpClient();

        var from = messageSentAt.AddHours(-1);
        var to = messageSentAt.AddHours(1);
        var response = await client.GetAsync(
            Testing.Endpoints.Releases.Summary(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseStrictJson()
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();

        response = await client.GetAsync(
            Testing.Endpoints.Releases.Buckets(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Unit(Units.Hour))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseMethodName(
                $"{nameof(WhenMultipleFinalisationForDifferentMrn_AndOneOutsideFromAndTo_ShouldBeSingleCount)}_buckets"
            )
            .UseStrictJson()
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();

        response = await client.GetAsync(
            Testing.Endpoints.Releases.Data(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.ReleaseType(ReleaseType.Automatic))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseMethodName(
                $"{nameof(WhenMultipleFinalisationForDifferentMrn_AndOneOutsideFromAndTo_ShouldBeSingleCount)}_data"
            )
            .UseStrictJson()
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();
    }

    [Theory]
    [InlineData(Units.Hour)]
    [InlineData(Units.Day)]
    public async Task WhenMultipleFinalisationForDifferentMrn_ShouldBeExpectedBuckets(string unit)
    {
        var mrn1 = Guid.NewGuid().ToString();
        var mrn2 = Guid.NewGuid().ToString();
        var messageSentAt = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);
        var resourceEvent1 = CreateResourceEvent(
            mrn1,
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
            mrn2,
            ResourceEventResourceTypes.CustomsDeclaration,
            new CustomsDeclaration
            {
                Finalisation = new Finalisation
                {
                    ExternalVersion = 1,
                    FinalState = "0",
                    IsManualRelease = false,
                    MessageSentAt = messageSentAt.AddHours(2),
                },
            },
            ResourceEventSubResourceTypes.Finalisation
        );

        await SendMessage(resourceEvent1, CreateMessageAttributes(resourceEvent1));
        await SendMessage(resourceEvent2, CreateMessageAttributes(resourceEvent2));
        await WaitForFinalisationMrn(mrn1);
        await WaitForFinalisationMrn(mrn2);

        var client = CreateHttpClient();

        var from = messageSentAt.AddHours(-1);
        var to = messageSentAt.AddHours(3);
        var response = await client.GetAsync(
            Testing.Endpoints.Releases.Summary(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseParameters(unit)
            .UseStrictJson()
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();

        response = await client.GetAsync(
            Testing.Endpoints.Releases.Buckets(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Unit(unit))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseMethodName($"{nameof(WhenMultipleFinalisationForDifferentMrn_ShouldBeExpectedBuckets)}_buckets")
            .UseParameters(unit)
            .UseStrictJson()
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();
    }
}
