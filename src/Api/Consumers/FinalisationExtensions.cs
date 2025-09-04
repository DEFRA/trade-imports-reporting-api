using Defra.TradeImportsReportingApi.Api.Data.Entities;
using MongoDB.Bson;

namespace Defra.TradeImportsReportingApi.Api.Consumers;

public static class FinalisationExtensions
{
    public static Finalisation ToFinalisation(
        this TradeImportsDataApi.Domain.CustomsDeclaration.Finalisation finalisation,
        string mrn
    )
    {
        if (finalisation.MessageSentAt.Kind is not DateTimeKind.Utc)
            throw new ArgumentException("MessageSentAt must be UTC");

        return new Finalisation
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Timestamp = finalisation.MessageSentAt,
            Mrn = mrn,
            ReleaseType = finalisation switch
            {
                { IsManualRelease: false, FinalState: not "1" and not "2" } => ReleaseType.Automatic,
                { IsManualRelease: true } => ReleaseType.Manual,
                _ => ReleaseType.Unknown,
            },
        };
    }

    public static bool ShouldBeStored(this Finalisation finalisation) =>
        finalisation.ReleaseType is ReleaseType.Manual or ReleaseType.Automatic;
}
