using Argon;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDataApi.Domain.Ipaffs;
using Defra.TradeImportsReportingApi.Api.Data;
using Defra.TradeImportsReportingApi.Api.Models;
using Defra.TradeImportsReportingApi.Testing;

namespace Defra.TradeImportsReportingApi.Api.IntegrationTests.Scenarios;

public class NotificationTests(SqsTestFixture sqsTestFixture) : ScenarioTestBase(sqsTestFixture)
{
    [Theory]
    [InlineData(ImportPreNotificationType.CVEDA)]
    [InlineData(ImportPreNotificationType.CVEDP)]
    [InlineData(ImportPreNotificationType.CHEDPP)]
    [InlineData(ImportPreNotificationType.CED)]
    public async Task WhenSingleNotification_ShouldBeSingleCount(string importNotificationType)
    {
        var ched = Guid.NewGuid().ToString();
        var created = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);
        var resourceEvent = CreateResourceEvent(
            ched,
            ResourceEventResourceTypes.ImportPreNotification,
            new ImportPreNotificationEntity
            {
                Created = created,
                Updated = created,
                ImportPreNotification = new ImportPreNotification { ImportNotificationType = importNotificationType },
            }
        );

        await SendMessage(resourceEvent, CreateMessageAttributes(resourceEvent));
        await WaitForNotificationChed(ched);

        var client = CreateHttpClient();

        var from = created.AddHours(-1);
        var to = created.AddHours(1);
        var response = await client.GetAsync(
            Testing.Endpoints.NotificationsSummary.Get(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseParameters(importNotificationType)
            .UseStrictJson()
            .DontScrubDateTimes();

        // No endpoint yet for buckets, repository only, to assert expected time bucketing

        var repository = new ReportRepository(new MongoDbContext(GetMongoDatabase()));

        var buckets = await repository.GetNotificationsBuckets(from, to, CancellationToken.None);

        await Verify(buckets)
            .UseMethodName($"{nameof(WhenSingleNotification_ShouldBeSingleCount)}_buckets")
            .UseParameters(importNotificationType)
            .AddExtraSettings(x => x.DefaultValueHandling = DefaultValueHandling.Include)
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();
    }

    [Fact]
    public async Task WhenMultipleNotificationForSameChed_ShouldBeSingleCount()
    {
        var ched = Guid.NewGuid().ToString();
        var created = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);
        var resourceEvent1 = CreateResourceEvent(
            ched,
            ResourceEventResourceTypes.ImportPreNotification,
            new ImportPreNotificationEntity
            {
                Created = created,
                Updated = created,
                ImportPreNotification = new ImportPreNotification
                {
                    ImportNotificationType = ImportPreNotificationType.CVEDA,
                },
            }
        );
        var resourceEvent2 = CreateResourceEvent(
            ched,
            ResourceEventResourceTypes.ImportPreNotification,
            new ImportPreNotificationEntity
            {
                Created = created,
                Updated = created.AddMinutes(1), // Different timestamp
                ImportPreNotification = new ImportPreNotification
                {
                    ImportNotificationType = ImportPreNotificationType.CVEDA,
                },
            }
        );

        await SendMessage(resourceEvent1, CreateMessageAttributes(resourceEvent1));
        await SendMessage(resourceEvent2, CreateMessageAttributes(resourceEvent2));
        await WaitForNotificationChed(ched, count: 2);

        var client = CreateHttpClient();

        var from = created.AddHours(-1);
        var to = created.AddHours(1);
        var response = await client.GetAsync(
            Testing.Endpoints.NotificationsSummary.Get(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync()).UseStrictJson().DontScrubDateTimes();

        // No endpoint yet for buckets, repository only, to assert expected time bucketing

        var repository = new ReportRepository(new MongoDbContext(GetMongoDatabase()));

        var buckets = await repository.GetNotificationsBuckets(from, to, CancellationToken.None);

        await Verify(buckets)
            .UseMethodName($"{nameof(WhenMultipleNotificationForSameChed_ShouldBeSingleCount)}_buckets")
            .AddExtraSettings(x => x.DefaultValueHandling = DefaultValueHandling.Include)
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();
    }

    [Fact]
    public async Task WhenMultipleNotificationForDifferentChed_AndOneOutsideFromAndTo_ShouldBeSingleCount()
    {
        var ched1 = Guid.NewGuid().ToString();
        var ched2 = Guid.NewGuid().ToString();
        var created = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);
        var resourceEvent1 = CreateResourceEvent(
            ched1,
            ResourceEventResourceTypes.ImportPreNotification,
            new ImportPreNotificationEntity
            {
                Created = created,
                Updated = created,
                ImportPreNotification = new ImportPreNotification
                {
                    ImportNotificationType = ImportPreNotificationType.CVEDA,
                },
            }
        );
        var resourceEvent2 = CreateResourceEvent(
            ched2,
            ResourceEventResourceTypes.ImportPreNotification,
            new ImportPreNotificationEntity
            {
                Created = created.AddHours(2), // Outside From and To,
                Updated = created.AddHours(2).AddMinutes(1),
                ImportPreNotification = new ImportPreNotification
                {
                    ImportNotificationType = ImportPreNotificationType.CVEDA,
                },
            }
        );

        await SendMessage(resourceEvent1, CreateMessageAttributes(resourceEvent1));
        await SendMessage(resourceEvent2, CreateMessageAttributes(resourceEvent2));
        await WaitForNotificationChed(ched1);
        await WaitForNotificationChed(ched2);

        var client = CreateHttpClient();

        var from = created.AddHours(-1);
        var to = created.AddHours(1);
        var response = await client.GetAsync(
            Testing.Endpoints.NotificationsSummary.Get(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync()).UseStrictJson().DontScrubDateTimes();

        // No endpoint yet for buckets, repository only, to assert expected time bucketing

        var repository = new ReportRepository(new MongoDbContext(GetMongoDatabase()));

        var buckets = await repository.GetNotificationsBuckets(from, to, CancellationToken.None);

        await Verify(buckets)
            .UseMethodName(
                $"{nameof(WhenMultipleNotificationForDifferentChed_AndOneOutsideFromAndTo_ShouldBeSingleCount)}_buckets"
            )
            .AddExtraSettings(x => x.DefaultValueHandling = DefaultValueHandling.Include)
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();
    }

    [Fact]
    public async Task WhenMultipleNotificationForDifferentChed_ShouldBeTwoBuckets()
    {
        var ched1 = Guid.NewGuid().ToString();
        var ched2 = Guid.NewGuid().ToString();
        var created = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);
        var resourceEvent1 = CreateResourceEvent(
            ched1,
            ResourceEventResourceTypes.ImportPreNotification,
            new ImportPreNotificationEntity
            {
                Created = created,
                Updated = created,
                ImportPreNotification = new ImportPreNotification
                {
                    ImportNotificationType = ImportPreNotificationType.CVEDA,
                },
            }
        );
        var resourceEvent2 = CreateResourceEvent(
            ched2,
            ResourceEventResourceTypes.ImportPreNotification,
            new ImportPreNotificationEntity
            {
                Created = created.AddHours(2), // Outside From and To,
                Updated = created.AddHours(2).AddMinutes(1),
                ImportPreNotification = new ImportPreNotification
                {
                    ImportNotificationType = ImportPreNotificationType.CVEDA,
                },
            }
        );

        await SendMessage(resourceEvent1, CreateMessageAttributes(resourceEvent1));
        await SendMessage(resourceEvent2, CreateMessageAttributes(resourceEvent2));
        await WaitForNotificationChed(ched1);
        await WaitForNotificationChed(ched2);

        var client = CreateHttpClient();

        var from = created.AddHours(-1);
        var to = created.AddHours(3);
        var response = await client.GetAsync(
            Testing.Endpoints.NotificationsSummary.Get(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync()).UseStrictJson().DontScrubDateTimes();

        // No endpoint yet for buckets, repository only, to assert expected time bucketing

        var repository = new ReportRepository(new MongoDbContext(GetMongoDatabase()));

        var buckets = await repository.GetNotificationsBuckets(from, to, CancellationToken.None);

        await Verify(buckets)
            .UseMethodName($"{nameof(WhenMultipleNotificationForDifferentChed_ShouldBeTwoBuckets)}_buckets")
            .AddExtraSettings(x => x.DefaultValueHandling = DefaultValueHandling.Include)
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();
    }
}
