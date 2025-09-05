using Argon;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsReportingApi.Api.Data;
using Defra.TradeImportsReportingApi.Testing;

namespace Defra.TradeImportsReportingApi.Api.IntegrationTests.Scenarios;

public class FinalisationTests(SqsTestFixture sqsTestFixture) : ScenarioTestBase(sqsTestFixture)
{
    [Fact]
    public async Task WhenSingleFinalisation_ShouldBeSingleCount()
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
                    FinalState = "0",
                    IsManualRelease = false,
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
            Testing.Endpoints.ReleasesSummary.Get(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync()).UseStrictJson().DontScrubDateTimes();

        // No endpoint yet for buckets, repository only, to assert expected time bucketing

        var repository = new ReportRepository(new MongoDbContext(GetMongoDatabase()));

        var buckets = await repository.GetReleasesBuckets(from, to, CancellationToken.None);

        await Verify(buckets)
            .UseMethodName($"{nameof(WhenSingleFinalisation_ShouldBeSingleCount)}_buckets")
            .AddExtraSettings(x => x.DefaultValueHandling = DefaultValueHandling.Include)
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
                    MessageSentAt = messageSentAt.AddMinutes(5), // Different message sent at
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
            Testing.Endpoints.ReleasesSummary.Get(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync()).UseStrictJson().DontScrubDateTimes();

        // No endpoint yet for buckets, repository only, to assert expected time bucketing

        var repository = new ReportRepository(new MongoDbContext(GetMongoDatabase()));

        var buckets = await repository.GetReleasesBuckets(from, to, CancellationToken.None);

        await Verify(buckets)
            .UseMethodName($"{nameof(WhenMultipleFinalisationForSameMrn_ShouldBeSingleCount)}_buckets")
            .AddExtraSettings(x => x.DefaultValueHandling = DefaultValueHandling.Include)
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();
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
                    MessageSentAt = messageSentAt.AddMinutes(5), // Different message sent at
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
            Testing.Endpoints.ReleasesSummary.Get(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync()).UseStrictJson().DontScrubDateTimes();

        // No endpoint yet for buckets, repository only, to assert expected time bucketing

        var repository = new ReportRepository(new MongoDbContext(GetMongoDatabase()));

        var buckets = await repository.GetReleasesBuckets(from, to, CancellationToken.None);

        await Verify(buckets)
            .UseMethodName(
                $"{nameof(WhenMultipleFinalisationForSameMrn_AndChangeFromAutomaticToManual_ShouldBeSingleCount)}_buckets"
            )
            .AddExtraSettings(x => x.DefaultValueHandling = DefaultValueHandling.Include)
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();
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
            Testing.Endpoints.ReleasesSummary.Get(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync()).UseStrictJson().DontScrubDateTimes();

        // No endpoint yet for buckets, repository only, to assert expected time bucketing

        var repository = new ReportRepository(new MongoDbContext(GetMongoDatabase()));

        var buckets = await repository.GetReleasesBuckets(from, to, CancellationToken.None);

        await Verify(buckets)
            .UseMethodName(
                $"{nameof(WhenMultipleFinalisationForDifferentMrn_AndOneOutsideFromAndTo_ShouldBeSingleCount)}_buckets"
            )
            .AddExtraSettings(x => x.DefaultValueHandling = DefaultValueHandling.Include)
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();
    }

    [Fact]
    public async Task WhenMultipleFinalisationForDifferentMrn_ShouldBeTwoBuckets()
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
            Testing.Endpoints.ReleasesSummary.Get(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync()).UseStrictJson().DontScrubDateTimes();

        // No endpoint yet for buckets, repository only, to assert expected time bucketing

        var repository = new ReportRepository(new MongoDbContext(GetMongoDatabase()));

        var buckets = await repository.GetReleasesBuckets(from, to, CancellationToken.None);

        await Verify(buckets)
            .UseMethodName($"{nameof(WhenMultipleFinalisationForDifferentMrn_ShouldBeTwoBuckets)}_buckets")
            .AddExtraSettings(x => x.DefaultValueHandling = DefaultValueHandling.Include)
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();
    }
}
