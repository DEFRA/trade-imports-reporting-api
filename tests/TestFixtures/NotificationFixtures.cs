using AutoFixture;
using AutoFixture.Dsl;
using Defra.TradeImportsDataApi.Domain.Ipaffs;

namespace Defra.TradeImportsReportingApi.TestFixtures;

public static class NotificationFixtures
{
    private static Fixture GetFixture()
    {
        var fixture = new Fixture();

        fixture.Customize<DateOnly>(o => o.FromFactory((DateTime dt) => DateOnly.FromDateTime(dt)));

        return fixture;
    }

    public static IPostprocessComposer<ImportPreNotification> ImportPreNotificationFixture()
    {
        return GetFixture().Build<ImportPreNotification>();
    }
}
