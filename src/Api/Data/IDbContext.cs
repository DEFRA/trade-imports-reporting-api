using Defra.TradeImportsReportingApi.Api.Data.Entities;
using MongoDB.Driver;

namespace Defra.TradeImportsReportingApi.Api.Data;

public interface IDbContext
{
    IMongoCollection<Finalisation> Finalisations { get; }
    IMongoCollection<Decision> Decisions { get; }
    IMongoCollection<Request> Requests { get; }
}
