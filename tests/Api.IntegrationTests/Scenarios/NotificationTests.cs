using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDataApi.Domain.Ipaffs;
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
            Testing.Endpoints.Notifications.Summary(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseParameters(importNotificationType)
            .UseStrictJson()
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();

        response = await client.GetAsync(
            Testing.Endpoints.Notifications.Buckets(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Unit(Units.Hour))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseMethodName($"{nameof(WhenSingleNotification_ShouldBeSingleCount)}_buckets")
            .UseParameters(importNotificationType)
            .UseStrictJson()
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
            Testing.Endpoints.Notifications.Summary(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseStrictJson()
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();

        response = await client.GetAsync(
            Testing.Endpoints.Notifications.Buckets(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Unit(Units.Hour))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseMethodName($"{nameof(WhenMultipleNotificationForSameChed_ShouldBeSingleCount)}_buckets")
            .UseStrictJson()
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
            Testing.Endpoints.Notifications.Summary(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseStrictJson()
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();

        response = await client.GetAsync(
            Testing.Endpoints.Notifications.Buckets(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Unit(Units.Hour))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseMethodName(
                $"{nameof(WhenMultipleNotificationForDifferentChed_AndOneOutsideFromAndTo_ShouldBeSingleCount)}_buckets"
            )
            .UseStrictJson()
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();
    }

    [Theory]
    [InlineData(Units.Hour, 2)]
    [InlineData(Units.Day, 1)]
    public async Task WhenMultipleNotificationForDifferentChed_ShouldBeExpectedBuckets(string unit, int expectedBuckets)
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
                    ImportNotificationType = ImportPreNotificationType.CVEDP,
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
            Testing.Endpoints.Notifications.Summary(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseParameters(unit, expectedBuckets)
            .UseStrictJson()
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();

        response = await client.GetAsync(
            Testing.Endpoints.Notifications.Buckets(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Unit(unit))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseMethodName($"{nameof(WhenMultipleNotificationForDifferentChed_ShouldBeExpectedBuckets)}_buckets")
            .UseParameters(unit, expectedBuckets)
            .UseStrictJson()
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();
    }
}
