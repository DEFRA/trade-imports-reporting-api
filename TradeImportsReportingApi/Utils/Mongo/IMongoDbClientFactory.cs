using MongoDB.Driver;

namespace TradeImportsReportingApi.Utils.Mongo;

public interface IMongoDbClientFactory
{
    IMongoClient GetClient();

    IMongoCollection<T> GetCollection<T>(string collection);
}