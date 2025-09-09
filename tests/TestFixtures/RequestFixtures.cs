using AutoFixture;
using AutoFixture.Dsl;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;

namespace Defra.TradeImportsReportingApi.TestFixtures;

public static class RequestFixtures
{
    private static Fixture GetFixture() => new();

    public static IPostprocessComposer<ClearanceRequest> ClearanceRequestFixture(DateTime? messageSentAt = null)
    {
        return GetFixture().Build<ClearanceRequest>().With(f => f.MessageSentAt, messageSentAt ?? DateTime.UtcNow);
    }
}
