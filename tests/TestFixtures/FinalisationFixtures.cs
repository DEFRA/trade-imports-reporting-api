using AutoFixture;
using AutoFixture.Dsl;
using Finalisation = Defra.TradeImportsDataApi.Domain.CustomsDeclaration.Finalisation;

namespace Defra.TradeImportsReportingApi.TestFixtures;

public static class FinalisationFixtures
{
    private static Fixture GetFixture() => new();

    public static IPostprocessComposer<Finalisation> FinalisationFixture(
        string? finalState = null,
        bool? isManualRelease = null,
        DateTime? messageSentAt = null
    )
    {
        return GetFixture()
            .Build<Finalisation>()
            .With(f => f.ExternalVersion, 1)
            .With(f => f.FinalState, finalState ?? "0")
            .With(f => f.IsManualRelease, isManualRelease ?? false)
            .With(f => f.MessageSentAt, messageSentAt ?? DateTime.UtcNow);
    }
}
