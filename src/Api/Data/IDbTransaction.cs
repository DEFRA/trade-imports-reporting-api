namespace Defra.TradeImportsReportingApi.Api.Data;

public interface IDbTransaction : IDisposable
{
    Task Commit(CancellationToken cancellationToken);
}
