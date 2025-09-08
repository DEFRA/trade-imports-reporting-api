using AutoFixture;
using Defra.TradeImportsReportingApi.Api.Consumers;
using Defra.TradeImportsReportingApi.Api.Data.Entities;
using Defra.TradeImportsReportingApi.Api.Models;
using Defra.TradeImportsReportingApi.TestFixtures;

namespace Defra.TradeImportsReportingApi.Api.Tests.Consumers;

public class NotificationExtensionsTests
{
    [Theory]
    [InlineData(ImportPreNotificationType.CVEDA, NotificationType.ChedA)]
    [InlineData(ImportPreNotificationType.CVEDP, NotificationType.ChedP)]
    [InlineData(ImportPreNotificationType.CHEDPP, NotificationType.ChedPP)]
    [InlineData(ImportPreNotificationType.CED, NotificationType.ChedD)]
    [InlineData(ImportPreNotificationType.IMP, NotificationType.Unknown)]
    public void ToNotification_NotificationType_ShouldBeAsExpected(string importNotificationType, string expected)
    {
        var importPreNotification = NotificationFixtures
            .ImportPreNotificationFixture()
            .With(x => x.ImportNotificationType, importNotificationType)
            .Create();

        importPreNotification
            .ToNotification("ched", DateTime.UtcNow, DateTime.UtcNow)
            .NotificationType.Should()
            .Be(expected);
    }

    [Fact]
    public async Task ToNotification_MapAsExpected()
    {
        var importPreNotification = NotificationFixtures
            .ImportPreNotificationFixture()
            .With(x => x.ImportNotificationType, ImportPreNotificationType.CVEDA)
            .Create();

        var subject = importPreNotification.ToNotification(
            "ched",
            new DateTime(2025, 7, 3, 13, 42, 0, DateTimeKind.Utc),
            new DateTime(2025, 7, 3, 14, 42, 0, DateTimeKind.Utc)
        );

        await Verify(subject).ScrubMember(nameof(Notification.Id)).DontScrubDateTimes();

        subject.Id.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(NotificationType.Unknown, false)]
    [InlineData(NotificationType.ChedA, true)]
    [InlineData(NotificationType.ChedP, true)]
    [InlineData(NotificationType.ChedPP, true)]
    [InlineData(NotificationType.ChedD, true)]
    public void ShouldBeStored_AsExpected(string notificationType, bool shouldStore)
    {
        var notification = NotificationEntityFixtures
            .NotificationFixture()
            .With(x => x.NotificationType, notificationType)
            .Create();

        notification.ShouldBeStored().Should().Be(shouldStore);
    }
}
