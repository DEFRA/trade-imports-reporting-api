using AutoFixture;
using AutoFixture.Dsl;
using Defra.TradeImportsReportingApi.Api.Data.Entities;

namespace Defra.TradeImportsReportingApi.TestFixtures;

public static class FinalisationEntityFixtures
{
    private static Fixture GetFixture() => new();

    public static IPostprocessComposer<Finalisation> FinalisationFixture()
    {
        return GetFixture().Build<Finalisation>();
    }
}
