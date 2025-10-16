using System.Diagnostics.CodeAnalysis;
using Defra.TradeImportsReportingApi.Api.Data.Entities;
using MongoDB.Driver;

namespace Defra.TradeImportsReportingApi.Api.Data;

[ExcludeFromCodeCoverage]
public class MongoDbContext(IMongoDatabase database) : IDbContext
{
    public IMongoCollection<Finalisation> Finalisations { get; } =
        database.GetCollection<Finalisation>(typeof(Finalisation).DataEntityName());

    public IMongoCollection<Decision> Decisions { get; } = database.GetCollection<Decision>(typeof(Decision).DataEntityName());

    public IMongoCollection<Request> Requests { get; } = database.GetCollection<Request>(typeof(Request).DataEntityName());

    public IMongoCollection<Notification> Notifications { get; } =
        database.GetCollection<Notification>(typeof(Notification).DataEntityName());
}
