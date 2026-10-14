namespace Defra.TradeImportsReportingApi.Api.Data;

public record ChedReservationsSummary(
    int Unreserved,
    int Reserved,
    int Consumed,
    int Unsuccessful,
    int Deleted,
    int Total
)
{
    public static ChedReservationsSummary Empty => new(0, 0, 0, 0, 0, 0);
}
