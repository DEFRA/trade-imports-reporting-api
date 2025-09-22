using Defra.TradeImportsReportingApi.Api.Models;
using Defra.TradeImportsReportingApi.Testing;

namespace Defra.TradeImportsReportingApi.Api.IntegrationTests.Scenarios.Notifications;

public class NotificationTests(SqsTestFixture sqsTestFixture) : ScenarioTestBase(sqsTestFixture)
{
    [Theory]
    [InlineData(ImportPreNotificationType.CVEDA)]
    [InlineData(ImportPreNotificationType.CVEDP)]
    [InlineData(ImportPreNotificationType.CHEDPP)]
    [InlineData(ImportPreNotificationType.CED)]
    public async Task WhenSingleNotification_ShouldBeSingleCount(string importNotificationType)
    {
        var created = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);

        await SendNotification(created, type: importNotificationType);

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

        response = await client.GetAsync(
            Testing.Endpoints.Notifications.Intervals(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Intervals(CreateIntervals(from, to, 2)))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseMethodName($"{nameof(WhenSingleNotification_ShouldBeSingleCount)}_intervals")
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

        await SendNotification(created, ched, wait: false);
        // Different timestamp
        await SendNotification(created, ched, updated: created.AddMinutes(1), wait: false);
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

        response = await client.GetAsync(
            Testing.Endpoints.Notifications.Intervals(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Intervals(CreateIntervals(from, to, 2)))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseMethodName($"{nameof(WhenMultipleNotificationForSameChed_ShouldBeSingleCount)}_intervals")
            .UseStrictJson()
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();
    }

    [Fact]
    public async Task WhenMultipleNotificationForDifferentChed_AndOneOutsideFromAndTo_ShouldBeSingleCount()
    {
        var created = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);

        await SendNotification(created);
        // Outside From and To
        await SendNotification(created.AddHours(2), updated: created.AddHours(2).AddMinutes(1));

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

        response = await client.GetAsync(
            Testing.Endpoints.Notifications.Intervals(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Intervals(CreateIntervals(from, to, 2)))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseMethodName(
                $"{nameof(WhenMultipleNotificationForDifferentChed_AndOneOutsideFromAndTo_ShouldBeSingleCount)}_intervals"
            )
            .UseStrictJson()
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();
    }

    [Theory]
    [InlineData(Units.Hour)]
    [InlineData(Units.Day)]
    public async Task WhenMultipleNotificationForDifferentChed_ShouldBeExpectedBuckets(string unit)
    {
        var created = new DateTime(2025, 9, 3, 16, 8, 0, DateTimeKind.Utc);

        await SendNotification(created);
        // Outside From and To
        await SendNotification(
            created.AddHours(2),
            updated: created.AddHours(2).AddMinutes(1),
            type: ImportPreNotificationType.CVEDP
        );

        var client = CreateHttpClient();

        var from = created.AddHours(-1);
        var to = created.AddHours(3);
        var response = await client.GetAsync(
            Testing.Endpoints.Notifications.Summary(
                EndpointQuery.New.Where(EndpointFilter.From(from)).Where(EndpointFilter.To(to))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseParameters(unit)
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
            .UseParameters(unit)
            .UseStrictJson()
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();

        response = await client.GetAsync(
            Testing.Endpoints.Notifications.Intervals(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Intervals(CreateIntervals(from, to, 2)))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseMethodName($"{nameof(WhenMultipleNotificationForDifferentChed_ShouldBeExpectedBuckets)}_intervals")
            .UseParameters(unit)
            .UseStrictJson()
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();
    }

    [Fact]
    public async Task WhenCallingFor24Hours_ShouldBeExpectedBuckets()
    {
        var start = new DateTime(2025, 9, 3, 0, 0, 0, DateTimeKind.Utc);

        // First bucket
        await SendNotification(start);
        await SendNotification(start.AddMinutes(1), type: ImportPreNotificationType.CVEDP);
        // Second bucket
        await SendNotification(start.AddHours(13));
        await SendNotification(start.AddHours(15), type: ImportPreNotificationType.CVEDP);
        await SendNotification(start.AddHours(17), type: ImportPreNotificationType.CHEDPP);
        await SendNotification(start.AddHours(19), type: ImportPreNotificationType.CED);
        // Returned in the second request (see below) as would be the next day based on
        // 2025-09-03 00:00:00 to 2025-09-04 00:00:00
        await SendNotification(start.AddHours(24));

        var client = CreateHttpClient();

        var from = start;
        var to = start.AddDays(1);

        // First request
        var response = await client.GetAsync(
            Testing.Endpoints.Notifications.Intervals(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Intervals([from.AddHours(12)]))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseStrictJson()
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();

        from = from.AddDays(1);
        to = to.AddDays(1);

        // Second request
        response = await client.GetAsync(
            Testing.Endpoints.Notifications.Intervals(
                EndpointQuery
                    .New.Where(EndpointFilter.From(from))
                    .Where(EndpointFilter.To(to))
                    .Where(EndpointFilter.Intervals([from.AddHours(12)]))
            )
        );

        await VerifyJson(await response.Content.ReadAsStringAsync())
            .UseMethodName($"{nameof(WhenCallingFor24Hours_ShouldBeExpectedBuckets)}_second")
            .UseStrictJson()
            .DontScrubDateTimes()
            .DontIgnoreEmptyCollections();
    }
}
