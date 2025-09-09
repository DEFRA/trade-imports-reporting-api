using System.Diagnostics.CodeAnalysis;
using Defra.TradeImportsReportingApi.Api.Data.Entities;
using MongoDB.Driver;

namespace Defra.TradeImportsReportingApi.Api.Data;

[ExcludeFromCodeCoverage]
public class MongoIndexService(IMongoDatabase database, ILogger<MongoIndexService> logger) : IHostedService
{
    private const string TimestampIdx = "TimestampIdx";
    private const string MatchIdx = "MatchIdx";

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
                .Descending(x => x.Timestamp),
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
                .Descending(x => x.Timestamp),
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
        try
        {
            var indexModel = new CreateIndexModel<T>(
                keys,
                new CreateIndexOptions
                {
                    Name = name,
                    Background = true,
                    Unique = unique,
                }
            );
            await database
                .GetCollection<T>(typeof(T).Name)
                .Indexes.CreateOneAsync(indexModel, cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to Create index {Name} on {Collection}", name, typeof(T).Name);
        }
    }
}
