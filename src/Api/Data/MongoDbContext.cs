using System.Diagnostics.CodeAnalysis;
using Defra.TradeImportsReportingApi.Api.Data.Entities;
using MongoDB.Driver;

namespace Defra.TradeImportsReportingApi.Api.Data;

[ExcludeFromCodeCoverage]
public class MongoDbContext(IMongoDatabase database) : IDbContext
{
    public IMongoCollection<Finalisation> Finalisations { get; } =
        database.GetCollection<Finalisation>(nameof(Finalisation));

    public IMongoCollection<Decision> Decisions { get; } = database.GetCollection<Decision>(nameof(Decision));

    public IMongoCollection<Request> Requests { get; } = database.GetCollection<Request>(nameof(Request));

    public IMongoCollection<Notification> Notifications { get; } =
        database.GetCollection<Notification>(nameof(Notification));
}
