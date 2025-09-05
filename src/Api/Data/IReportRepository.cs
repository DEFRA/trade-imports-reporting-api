namespace Defra.TradeImportsReportingApi.Api.Data;

public interface IReportRepository
{
    Task<ReleasesSummary> GetReleasesSummary(DateTime from, DateTime to, CancellationToken cancellationToken);

    Task<IReadOnlyList<ReleasesBucket>> GetReleasesBuckets(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken
    );

    Task<MatchesSummary> GetMatchesSummary(DateTime from, DateTime to, CancellationToken cancellationToken);

    Task<IReadOnlyList<MatchesBucket>> GetMatchesBuckets(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken
    );

    Task<ClearanceRequestsSummary> GetClearanceRequestsSummary(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken
    );
}
