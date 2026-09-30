using AutoFixture;
using AutoFixture.Dsl;
using Defra.TradeImportsReportingApi.Api.Data.Entities;

namespace Defra.TradeImportsReportingApi.TestFixtures;

public static class TracesChedEntityFixtures
{
    private static Fixture GetFixture() => new();

    public static IPostprocessComposer<TracesChed> TracesChedFixture()
    {
        return GetFixture().Build<TracesChed>();
    }
}
