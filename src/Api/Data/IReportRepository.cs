namespace Defra.TradeImportsReportingApi.Api.Data;

public interface IReportRepository
{
    Task<ReleasesSummary> GetReleasesSummary(DateTime from, DateTime to, CancellationToken cancellationToken);

    Task<IReadOnlyList<ReleasesBucket>> GetReleasesBuckets(
        DateTime from,
        DateTime to,
        string unit,
        CancellationToken cancellationToken
    );

    Task<MatchesSummary> GetMatchesSummary(DateTime from, DateTime to, CancellationToken cancellationToken);

    Task<IReadOnlyList<MatchesBucket>> GetMatchesBuckets(
        DateTime from,
        DateTime to,
        string unit,
        CancellationToken cancellationToken
    );

    Task<ClearanceRequestsSummary> GetClearanceRequestsSummary(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<ClearanceRequestsBucket>> GetClearanceRequestsBuckets(
        DateTime from,
        DateTime to,
        string unit,
        CancellationToken cancellationToken
    );

    Task<NotificationsSummary> GetNotificationsSummary(DateTime from, DateTime to, CancellationToken cancellationToken);

    Task<IReadOnlyList<NotificationsBucket>> GetNotificationsBuckets(
        DateTime from,
        DateTime to,
        string unit,
        CancellationToken cancellationToken
    );

    Task<LastReceivedSummary> GetLastReceivedSummary(CancellationToken cancellationToken);
}
