using Argon;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsReportingApi.Api.Data;
using Defra.TradeImportsReportingApi.Api.Data.Entities;
using Defra.TradeImportsReportingApi.Api.Models;
using Defra.TradeImportsReportingApi.Testing;

namespace Defra.TradeImportsReportingApi.Api.IntegrationTests.Scenarios;

public class DecisionTests(SqsTestFixture sqsTestFixture) : ScenarioTestBase(sqsTestFixture)
{
    [Theory]
    [InlineData(DecisionCode.NoMatch)]
    [InlineData(DecisionCode.Match)]
    public async Task WhenSingleDecision_ShouldBeSingleCount(string decisionCode)
    {
        var mrn = Guid.NewGuid().ToString();
        var mrnCreated = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);
        var resourceEvent = CreateResourceEvent(
            mrn,
            ResourceEventResourceTypes.CustomsDeclaration,
            new CustomsDeclarationEntity
            {
                Created = mrnCreated,
                ClearanceDecision = new ClearanceDecision
                {
                    Created = mrnCreated.AddSeconds(20),
                    Items =
                    [
                        new ClearanceDecisionItem
                        {
                            Checks = [new ClearanceDecisionCheck { CheckCode = "IGNORE", DecisionCode = decisionCode }],
                        },
                    ],
                },
            },
            ResourceEventSubResourceTypes.ClearanceDecision
        );

        await SendMessage(resourceEvent, CreateMessageAttributes(resourceEvent));
        await WaitForDecisionMrn(mrn);

        var client = CreateHttpClient();

        var from = mrnCreated.AddHours(-1);
        var to = mrnCreated.AddHours(1);
        var response = await client.GetAsync(
            Testing.Endpoints.MatchesSummary.Get(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseParameters(decisionCode)
            .UseStrictJson()
            .DontScrubDateTimes();

        // No endpoint yet for buckets, repository only, to assert expected time bucketing

        var repository = new ReportRepository(new MongoDbContext(GetMongoDatabase()));

        var buckets = await repository.GetMatchesBuckets(from, to, CancellationToken.None);

        await Verify(buckets)
            .UseMethodName($"{nameof(WhenSingleDecision_ShouldBeSingleCount)}_buckets")
            .UseParameters(decisionCode)
            .AddExtraSettings(x => x.DefaultValueHandling = DefaultValueHandling.Include)
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();
    }

    [Fact]
    public async Task WhenMultipleDecisionForSameMrn_ShouldBeSingleCount()
    {
        var mrn = Guid.NewGuid().ToString();
        var mrnCreated = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);
        var resourceEvent1 = CreateResourceEvent(
            mrn,
            ResourceEventResourceTypes.CustomsDeclaration,
            new CustomsDeclarationEntity
            {
                Created = mrnCreated,
                ClearanceDecision = new ClearanceDecision
                {
                    Created = mrnCreated.AddSeconds(20),
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
        var resourceEvent2 = CreateResourceEvent(
            mrn,
            ResourceEventResourceTypes.CustomsDeclaration,
            new CustomsDeclarationEntity
            {
                Created = mrnCreated,
                ClearanceDecision = new ClearanceDecision
                {
                    Created = mrnCreated.AddSeconds(40), // Different timestamp
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

        await SendMessage(resourceEvent1, CreateMessageAttributes(resourceEvent1));
        await SendMessage(resourceEvent2, CreateMessageAttributes(resourceEvent2));
        await WaitForDecisionMrn(mrn, count: 2);

        var client = CreateHttpClient();

        var from = mrnCreated.AddHours(-1);
        var to = mrnCreated.AddHours(1);
        var response = await client.GetAsync(
            Testing.Endpoints.MatchesSummary.Get(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync()).UseStrictJson().DontScrubDateTimes();

        // No endpoint yet for buckets, repository only, to assert expected time bucketing

        var repository = new ReportRepository(new MongoDbContext(GetMongoDatabase()));

        var buckets = await repository.GetMatchesBuckets(from, to, CancellationToken.None);

        await Verify(buckets)
            .UseMethodName($"{nameof(WhenMultipleDecisionForSameMrn_ShouldBeSingleCount)}_buckets")
            .AddExtraSettings(x => x.DefaultValueHandling = DefaultValueHandling.Include)
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();
    }

    [Fact]
    public async Task WhenMultipleDecisionForSameMrn_AndChangeFromNoMatchToMatch_ShouldBeSingleCount()
    {
        var mrn = Guid.NewGuid().ToString();
        var mrnCreated = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);
        var resourceEvent1 = CreateResourceEvent(
            mrn,
            ResourceEventResourceTypes.CustomsDeclaration,
            new CustomsDeclarationEntity
            {
                Created = mrnCreated,
                ClearanceDecision = new ClearanceDecision
                {
                    Created = mrnCreated.AddSeconds(20),
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
        var resourceEvent2 = CreateResourceEvent(
            mrn,
            ResourceEventResourceTypes.CustomsDeclaration,
            new CustomsDeclarationEntity
            {
                Created = mrnCreated,
                ClearanceDecision = new ClearanceDecision
                {
                    Created = mrnCreated.AddSeconds(40), // Different timestamp
                    Items =
                    [
                        new ClearanceDecisionItem
                        {
                            Checks =
                            [
                                new ClearanceDecisionCheck
                                {
                                    CheckCode = "IGNORE",
                                    DecisionCode = DecisionCode.Match, // Change from NoMatch to Match
                                },
                            ],
                        },
                    ],
                },
            },
            ResourceEventSubResourceTypes.ClearanceDecision
        );

        await SendMessage(resourceEvent1, CreateMessageAttributes(resourceEvent1));
        await SendMessage(resourceEvent2, CreateMessageAttributes(resourceEvent2));
        await WaitForDecisionMrn(mrn, count: 2);

        var client = CreateHttpClient();

        var from = mrnCreated.AddHours(-1);
        var to = mrnCreated.AddHours(1);
        var response = await client.GetAsync(
            Testing.Endpoints.MatchesSummary.Get(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync()).UseStrictJson().DontScrubDateTimes();

        // No endpoint yet for buckets, repository only, to assert expected time bucketing

        var repository = new ReportRepository(new MongoDbContext(GetMongoDatabase()));

        var buckets = await repository.GetMatchesBuckets(from, to, CancellationToken.None);

        await Verify(buckets)
            .UseMethodName(
                $"{nameof(WhenMultipleDecisionForSameMrn_AndChangeFromNoMatchToMatch_ShouldBeSingleCount)}_buckets"
            )
            .AddExtraSettings(x => x.DefaultValueHandling = DefaultValueHandling.Include)
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();
    }

    [Fact]
    public async Task WhenMultipleDecisionForDifferentMrn_AndOneOutsideFromAndTo_ShouldBeSingleCount()
    {
        var mrn1 = Guid.NewGuid().ToString();
        var mrn2 = Guid.NewGuid().ToString();
        var mrnCreated = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);
        var resourceEvent1 = CreateResourceEvent(
            mrn1,
            ResourceEventResourceTypes.CustomsDeclaration,
            new CustomsDeclarationEntity
            {
                Created = mrnCreated,
                ClearanceDecision = new ClearanceDecision
                {
                    Created = mrnCreated.AddSeconds(20),
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
        var resourceEvent2 = CreateResourceEvent(
            mrn2,
            ResourceEventResourceTypes.CustomsDeclaration,
            new CustomsDeclarationEntity
            {
                Created = mrnCreated.AddHours(2), // Outside From and To,
                ClearanceDecision = new ClearanceDecision
                {
                    Created = mrnCreated.AddHours(2).AddSeconds(40),
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

        await SendMessage(resourceEvent1, CreateMessageAttributes(resourceEvent1));
        await SendMessage(resourceEvent2, CreateMessageAttributes(resourceEvent2));
        await WaitForDecisionMrn(mrn1);
        await WaitForDecisionMrn(mrn2);

        var client = CreateHttpClient();

        var from = mrnCreated.AddHours(-1);
        var to = mrnCreated.AddHours(1);
        var response = await client.GetAsync(
            Testing.Endpoints.MatchesSummary.Get(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync()).UseStrictJson().DontScrubDateTimes();

        // No endpoint yet for buckets, repository only, to assert expected time bucketing

        var repository = new ReportRepository(new MongoDbContext(GetMongoDatabase()));

        var buckets = await repository.GetMatchesBuckets(from, to, CancellationToken.None);

        await Verify(buckets)
            .UseMethodName(
                $"{nameof(WhenMultipleDecisionForDifferentMrn_AndOneOutsideFromAndTo_ShouldBeSingleCount)}_buckets"
            )
            .AddExtraSettings(x => x.DefaultValueHandling = DefaultValueHandling.Include)
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();
    }

    [Fact]
    public async Task WhenMultipleDecisionForDifferentMrn_ShouldBeTwoBuckets()
    {
        var mrn1 = Guid.NewGuid().ToString();
        var mrn2 = Guid.NewGuid().ToString();
        var mrnCreated = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);
        var resourceEvent1 = CreateResourceEvent(
            mrn1,
            ResourceEventResourceTypes.CustomsDeclaration,
            new CustomsDeclarationEntity
            {
                Created = mrnCreated,
                ClearanceDecision = new ClearanceDecision
                {
                    Created = mrnCreated.AddSeconds(20),
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
        var resourceEvent2 = CreateResourceEvent(
            mrn2,
            ResourceEventResourceTypes.CustomsDeclaration,
            new CustomsDeclarationEntity
            {
                Created = mrnCreated.AddHours(2),
                ClearanceDecision = new ClearanceDecision
                {
                    Created = mrnCreated.AddHours(2).AddSeconds(40),
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

        await SendMessage(resourceEvent1, CreateMessageAttributes(resourceEvent1));
        await SendMessage(resourceEvent2, CreateMessageAttributes(resourceEvent2));
        await WaitForDecisionMrn(mrn1);
        await WaitForDecisionMrn(mrn2);

        var client = CreateHttpClient();

        var from = mrnCreated.AddHours(-1);
        var to = mrnCreated.AddHours(3);
        var response = await client.GetAsync(
            Testing.Endpoints.MatchesSummary.Get(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync()).UseStrictJson().DontScrubDateTimes();

        // No endpoint yet for buckets, repository only, to assert expected time bucketing

        var repository = new ReportRepository(new MongoDbContext(GetMongoDatabase()));

        var buckets = await repository.GetMatchesBuckets(from, to, CancellationToken.None);

        await Verify(buckets)
            .UseMethodName($"{nameof(WhenMultipleDecisionForDifferentMrn_ShouldBeTwoBuckets)}_buckets")
            .AddExtraSettings(x => x.DefaultValueHandling = DefaultValueHandling.Include)
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();
    }
}
