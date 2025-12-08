using System.Diagnostics.CodeAnalysis;
using Defra.TradeImportsReportingApi.Api.Data.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Defra.TradeImportsReportingApi.Api.Data;

[ExcludeFromCodeCoverage]
public class MongoIndexService(IMongoDatabase database, ILogger<MongoIndexService> logger) : IHostedService
{
    private const string TimestampIdx = "TimestampIdx";
    private const string MatchIdx = "MatchIdx";
    private const string LatestMrnIdx = "LatestMrnIdx";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await CreateFinalisationIndexes(cancellationToken);
        await CreateDecisionIndexes(cancellationToken);
        await CreateRequestIndexes(cancellationToken);
        await CreateNotificationIndexes(cancellationToken);
    }

    private async Task CreateFinalisationIndexes(CancellationToken cancellationToken)
    {
        await CreateIndex(
            LatestMrnIdx,
            Builders<Finalisation>.IndexKeys.Ascending(x => x.Mrn).Descending(x => x.Timestamp),
            cancellationToken: cancellationToken
        );
        await CreateIndex(
            TimestampIdx,
            Builders<Finalisation>.IndexKeys.Ascending(x => x.Timestamp),
            cancellationToken: cancellationToken
        );
        await CreateIndex(
            MatchIdx,
            Builders<Finalisation>
                // Order of fields important - don't change without reason
                .IndexKeys.Ascending(x => x.Timestamp)
                .Ascending(x => x.ReleaseType)
                .Ascending(x => x.Mrn),
            cancellationToken: cancellationToken
        );
    }

    private async Task CreateDecisionIndexes(CancellationToken cancellationToken)
    {
        await CreateIndex(
            "MrnCreatedIdx",
            Builders<Decision>.IndexKeys.Ascending(x => x.MrnCreated),
            cancellationToken: cancellationToken
        );
        await CreateIndex(
            TimestampIdx,
            Builders<Decision>.IndexKeys.Ascending(x => x.Timestamp),
            cancellationToken: cancellationToken
        );
        await CreateIndex(
            MatchIdx,
            Builders<Decision>
                // Order of fields important - don't change without reason
                .IndexKeys.Ascending(x => x.MrnCreated)
                .Ascending(x => x.Mrn)
                .Descending(x => x.Timestamp)
                .Ascending(x => x.Match),
            cancellationToken: cancellationToken
        );
    }

    private async Task CreateRequestIndexes(CancellationToken cancellationToken)
    {
        await CreateIndex(
            TimestampIdx,
            Builders<Request>.IndexKeys.Ascending(x => x.Timestamp),
            cancellationToken: cancellationToken
        );
        await CreateIndex(
            MatchIdx,
            Builders<Request>
                // Order of fields important - don't change without reason
                .IndexKeys.Ascending(x => x.Timestamp)
                .Ascending(x => x.Mrn),
            cancellationToken: cancellationToken
        );
    }

    private async Task CreateNotificationIndexes(CancellationToken cancellationToken)
    {
        await CreateIndex(
            "NotificationCreatedIdx",
            Builders<Notification>.IndexKeys.Ascending(x => x.NotificationCreated),
            cancellationToken: cancellationToken
        );
        await CreateIndex(
            TimestampIdx,
            Builders<Notification>.IndexKeys.Ascending(x => x.Timestamp),
            cancellationToken: cancellationToken
        );
        await CreateIndex(
            MatchIdx,
            Builders<Notification>
                // Order of fields important - don't change without reason
                .IndexKeys.Ascending(x => x.NotificationCreated)
                .Ascending(x => x.ReferenceNumber)
                .Descending(x => x.Timestamp)
                .Ascending(x => x.NotificationType),
            cancellationToken: cancellationToken
        );
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task CreateIndex<T>(
        string name,
        IndexKeysDefinition<T> keys,
        bool unique = false,
        CancellationToken cancellationToken = default
    )
    {
        var collectionName = typeof(T).Name;

        try
        {
            var collection = database.GetCollection<T>(collectionName);
            var requestedKeys = keys.Render(
                new RenderArgs<T>(collection.DocumentSerializer, collection.Settings.SerializerRegistry)
            );

            using (var cursor = await collection.Indexes.ListAsync(cancellationToken))
            {
                var existingIndexes = await cursor.ToListAsync(cancellationToken);
                var existingByName = existingIndexes.FirstOrDefault(i => i.TryGetValue("name", out var n) && n == name);

                if (existingByName is not null)
                {
                    var existingKeys = existingByName.GetValue("key", new BsonDocument()).AsBsonDocument;
                    var existingUnique = existingByName.TryGetValue("unique", out var u) && u.IsBoolean && u.AsBoolean;

                    if (!existingKeys.Equals(requestedKeys) || existingUnique != unique)
                    {
                        logger.LogInformation(
                            "Updating index {Name} on {Collection}: keys/options differ. Dropping and recreating.",
                            name,
                            collectionName
                        );

                        await DropIndex(name, collection, cancellationToken);
                    }
                    else
                    {
                        // Index already exists and is correct
                        return;
                    }
                }
            }

            var indexModel = new CreateIndexModel<T>(
                keys,
                new CreateIndexOptions
                {
                    Name = name,
                    Background = true,
                    Unique = unique,
                }
            );

            await collection.Indexes.CreateOneAsync(indexModel, cancellationToken: cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to Create index {Name} on {Collection}", name, collectionName);
        }
    }

    private async Task DropIndex<T>(string name, IMongoCollection<T> collection, CancellationToken cancellationToken)
    {
        try
        {
            await collection.Indexes.DropOneAsync(name, cancellationToken);
        }
        catch (MongoCommandException mongoCommandException)
        {
            logger.LogWarning(
                mongoCommandException,
                "Index {Name} was not dropped on {Collection}. It may not exist.",
                name,
                collection.CollectionNamespace.CollectionName
            );
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Failed to drop index {Name} on {Collection}",
                name,
                collection.CollectionNamespace.CollectionName
            );
        }
    }
}
