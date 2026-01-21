using Defra.TradeImportsReportingApi.Api.Data.Entities;

namespace Defra.TradeImportsReportingApi.Api.Data;

public interface IReportRepository
{
    Task<ReleasesSummary> GetReleasesSummary(DateTime from, DateTime to, CancellationToken cancellationToken);

    Task<IReadOnlyList<ReleasesBucket>> GetReleasesIntervals(
        DateTime from,
        DateTime to,
        DateTime[] intervals,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<Finalisation>> GetReleases(
        DateTime from,
        DateTime to,
        string releaseType,
        CancellationToken cancellationToken
    );

    Task<MatchesSummary> GetMatchesSummary(DateTime from, DateTime to, CancellationToken cancellationToken);

    Task<IReadOnlyList<MatchesBucket>> GetMatchesIntervals(
        DateTime from,
        DateTime to,
        DateTime[] intervals,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<Decision>> GetMatches(
        DateTime from,
        DateTime to,
        bool match,
        CancellationToken cancellationToken
    );

    Task<ClearanceRequestsSummary> GetClearanceRequestsSummary(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<ClearanceRequestsBucket>> GetClearanceRequestsIntervals(
        DateTime from,
        DateTime to,
        DateTime[] intervals,
        CancellationToken cancellationToken
    );

    Task<NotificationsSummary> GetNotificationsSummary(DateTime from, DateTime to, CancellationToken cancellationToken);

    Task<IReadOnlyList<NotificationsBucket>> GetNotificationsIntervals(
        DateTime from,
        DateTime to,
        DateTime[] intervals,
        CancellationToken cancellationToken
    );

    Task<LastReceivedSummary> GetLastReceivedSummary(CancellationToken cancellationToken);

    Task<LastSentSummary> GetLastSentSummary(CancellationToken cancellationToken);

    Task<LastCreatedSummary> GetLastCreatedSummary(CancellationToken cancellationToken);
}
