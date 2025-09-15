// ReSharper disable InconsistentNaming
namespace Defra.TradeImportsReportingApi.Api.Data;

public record NotificationsSummary(int ChedA, int ChedP, int ChedPP, int ChedD, int Total)
{
    public static NotificationsSummary Empty => new(0, 0, 0, 0, 0);
}
