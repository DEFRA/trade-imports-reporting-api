using AutoFixture;
using AutoFixture.Dsl;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;

namespace Defra.TradeImportsReportingApi.TestFixtures;

public static class DecisionFixtures
{
    private static Fixture GetFixture() => new();

    public static IPostprocessComposer<ClearanceDecision> DecisionFixture()
    {
        return GetFixture().Build<ClearanceDecision>();
    }
}
