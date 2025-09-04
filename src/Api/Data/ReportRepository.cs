using Defra.TradeImportsReportingApi.Api.Data.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Defra.TradeImportsReportingApi.Api.Data;

public class ReportRepository(IDbContext dbContext) : IReportRepository
{
    public async Task<ReleasesSummary> GetReleasesSummary(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken
    )
    {
        var aggregatePipeline = new[]
        {
            new BsonDocument(
                "$match",
                new BsonDocument("timestamp", new BsonDocument { { "$gte", from }, { "$lt", to } })
            ),
            new BsonDocument(
                "$group",
                new BsonDocument
                {
                    { "_id", "$mrn" },
                    {
                        "latest",
                        new BsonDocument(
                            "$top",
                            new BsonDocument
                            {
                                { "sortBy", new BsonDocument("timestamp", -1) },
                                { "output", new BsonDocument("releaseType", "$releaseType") },
                            }
                        )
                    },
                }
            ),
            new BsonDocument(
                "$group",
                new BsonDocument
                {
                    { "_id", 1 },
                    {
                        "automatic",
                        new BsonDocument(
                            "$sum",
                            new BsonDocument(
                                "$cond",
                                new BsonArray
                                {
                                    new BsonDocument(
                                        "$eq",
                                        new BsonArray { "$latest.releaseType", ReleaseType.Automatic }
                                    ),
                                    1,
                                    0,
                                }
                            )
                        )
                    },
                    {
                        "manual",
                        new BsonDocument(
                            "$sum",
                            new BsonDocument(
                                "$cond",
                                new BsonArray
                                {
                                    new BsonDocument(
                                        "$eq",
                                        new BsonArray { "$latest.releaseType", ReleaseType.Manual }
                                    ),
                                    1,
                                    0,
                                }
                            )
                        )
                    },
                    { "total", new BsonDocument("$sum", 1) },
                }
            ),
            new BsonDocument(
                "$project",
                new BsonDocument
                {
                    { "_id", 0 },
                    { "automatic", 1 },
                    { "manual", 1 },
                    { "total", 1 },
                }
            ),
        };

        var aggregateTask = dbContext.Finalisations.AggregateAsync<ReleasesSummary>(
            aggregatePipeline,
            cancellationToken: cancellationToken
        );

        var results = await (await aggregateTask).ToListAsync(cancellationToken);

        return results.FirstOrDefault() ?? new ReleasesSummary(0, 0, 0);
    }

    public async Task<IReadOnlyList<ReleasesBucket>> GetReleasesBuckets(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken
    )
    {
        const string unit = "hour";

        var aggregatePipeline = new[]
        {
            new BsonDocument(
                "$match",
                new BsonDocument("timestamp", new BsonDocument { { "$gte", from }, { "$lt", to } })
            ),
            new BsonDocument(
                "$set",
                new BsonDocument
                {
                    {
                        "bucket",
                        new BsonDocument(
                            "$dateTrunc",
                            new BsonDocument
                            {
                                { "date", "$timestamp" },
                                { "unit", unit },
                                { "timezone", "UTC" },
                            }
                        )
                    },
                }
            ),
            new BsonDocument(
                "$group",
                new BsonDocument
                {
                    {
                        "_id",
                        new BsonDocument { { "bucket", "$bucket" }, { "mrn", "$mrn" } }
                    },
                    {
                        "latest",
                        new BsonDocument(
                            "$top",
                            new BsonDocument
                            {
                                { "sortBy", new BsonDocument("timestamp", -1) },
                                { "output", new BsonDocument("releaseType", "$releaseType") },
                            }
                        )
                    },
                }
            ),
            new BsonDocument(
                "$group",
                new BsonDocument
                {
                    {
                        "_id",
                        new BsonDocument { { "bucket", "$_id.bucket" } }
                    },
                    {
                        "automatic",
                        new BsonDocument(
                            "$sum",
                            new BsonDocument(
                                "$cond",
                                new BsonArray
                                {
                                    new BsonDocument(
                                        "$eq",
                                        new BsonArray { "$latest.releaseType", ReleaseType.Automatic }
                                    ),
                                    1,
                                    0,
                                }
                            )
                        )
                    },
                    {
                        "manual",
                        new BsonDocument(
                            "$sum",
                            new BsonDocument(
                                "$cond",
                                new BsonArray
                                {
                                    new BsonDocument(
                                        "$eq",
                                        new BsonArray { "$latest.releaseType", ReleaseType.Manual }
                                    ),
                                    1,
                                    0,
                                }
                            )
                        )
                    },
                    { "total", new BsonDocument("$sum", 1) },
                }
            ),
            new BsonDocument(
                "$project",
                new BsonDocument
                {
                    { "_id", 0 },
                    { "bucket", "$_id.bucket" },
                    {
                        "summary",
                        new BsonDocument
                        {
                            { "automatic", "$automatic" },
                            { "manual", "$manual" },
                            { "total", "$total" },
                        }
                    },
                }
            ),
            new BsonDocument("$sort", new BsonDocument { { "bucket", 1 } }),
        };

        var aggregateTask = dbContext.Finalisations.AggregateAsync<ReleasesBucket>(
            aggregatePipeline,
            cancellationToken: cancellationToken
        );

        return await (await aggregateTask).ToListAsync(cancellationToken);
    }
}
