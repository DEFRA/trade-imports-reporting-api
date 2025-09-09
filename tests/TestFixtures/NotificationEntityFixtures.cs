using AutoFixture;
using AutoFixture.Dsl;
using Defra.TradeImportsReportingApi.Api.Data.Entities;

namespace Defra.TradeImportsReportingApi.TestFixtures;

public static class NotificationEntityFixtures
{
    private static Fixture GetFixture() => new();

    public static IPostprocessComposer<Notification> NotificationFixture()
    {
        return GetFixture().Build<Notification>();
    }
}
