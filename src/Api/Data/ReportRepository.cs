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

        var aggregateTask = dbContext.Finalisations.AggregateAsync<BsonDocument>(
            aggregatePipeline,
            cancellationToken: cancellationToken
        );

        var result = await (await aggregateTask).FirstOrDefaultAsync(cancellationToken);

        if (result is null)
            return new ReleasesSummary(0, 0, 0);

        var automatic = result.GetValue("automatic", 0).ToInt32();
        var manual = result.GetValue("manual", 0).ToInt32();
        var total = result.GetValue("total", 0).ToInt32();

        return new ReleasesSummary(automatic, manual, total);
    }
}
