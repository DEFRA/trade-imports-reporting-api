using AutoFixture;
using AutoFixture.Dsl;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;

namespace Defra.TradeImportsReportingApi.TestFixtures;

public static class DecisionFixtures
{
    private static Fixture GetFixture() => new();

    public static IPostprocessComposer<ClearanceDecision> ClearanceDecisionFixture()
    {
        return GetFixture().Build<ClearanceDecision>();
    }
}
