using Defra.TradeImportsReportingApi.Api.Data.Entities;
using MongoDB.Driver;

namespace Defra.TradeImportsReportingApi.Api.Data;

public interface IDbContext
{
    IMongoCollection<Finalisation> Finalisations { get; }
}
