namespace Defra.TradeImportsReportingApi.Api.Data;

public interface IReportRepository
{
    Task<ReleasesSummary> GetReleasesSummary(DateTime from, DateTime to, CancellationToken cancellationToken);
}
