using Defra.TradeImportsReportingApi.Api.Data.Entities;

namespace Defra.TradeImportsReportingApi.Api.Data;

public interface IDbContext
{
    IMongoCollectionSet<RawMessageEntity> RawMessages { get; }

    Task SaveChanges(CancellationToken cancellationToken);

    Task StartTransaction(CancellationToken cancellationToken);

    Task CommitTransaction(CancellationToken cancellationToken);
}
