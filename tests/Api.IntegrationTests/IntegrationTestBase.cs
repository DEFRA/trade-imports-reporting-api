using System.Net.Http.Headers;
using Defra.TradeImportsReportingApi.Api.Data.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace Defra.TradeImportsReportingApi.Api.IntegrationTests;

[Trait("Category", "IntegrationTest")]
[Collection("Integration Tests")]
public abstract class IntegrationTestBase
{
    protected static HttpClient CreateHttpClient()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:8080") };
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic",
            // See compose.yml for username, password and scope configuration
            Convert.ToBase64String("IntegrationTests:integration-tests-pwd"u8.ToArray())
        );

        return httpClient;
    }

    private static IMongoDatabase GetMongoDatabase()
    {
        var settings = MongoClientSettings.FromConnectionString("mongodb://127.0.0.1:27017/?directConnection=true");
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
        settings.ConnectTimeout = TimeSpan.FromSeconds(5);
        settings.SocketTimeout = TimeSpan.FromSeconds(5);

        return new MongoClient(settings).GetDatabase("trade-imports-reporting-api");
    }

    protected static IMongoCollection<T> GetMongoCollection<T>()
    {
        var db = GetMongoDatabase();

        return db.GetCollection<T>(typeof(T).Name);
    }

    protected IntegrationTestBase()
    {
        var conventionPack = new ConventionPack
        {
            new CamelCaseElementNameConvention(),
            new EnumRepresentationConvention(BsonType.String),
        };

        ConventionRegistry.Register(nameof(conventionPack), conventionPack, _ => true);
    }
}
