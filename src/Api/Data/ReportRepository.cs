using System.Diagnostics.CodeAnalysis;
using Defra.TradeImportsReportingApi.Api.Data.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace Defra.TradeImportsReportingApi.Api.Data;

[ExcludeFromCodeCoverage]
[SuppressMessage(
    "SonarAnalyzer.CSharp",
    "S1192",
    Justification = "Specific BsonDocument format is used to ensure the correct query plan is executed as the "
        + "linq equivalent produces a plan that is inefficient. Having to replace all strings with "
        + "constants if they are repeated a certain number of times makes the code more difficult "
        + "to maintain. However, the field names have been extracted from each query so these are "
        + "free to change over time if needed."
)]
public class ReportRepository(IDbContext dbContext) : IReportRepository
{
    // The presence of the CamelCaseElementNameConvention denotes if we
    // are using camel case or not
    private static readonly bool s_camelCase = ConventionRegistry
        .Lookup(typeof(Finalisation))
        .Conventions.OfType<CamelCaseElementNameConvention>()
        .Any();

    private static class Fields
    {
        private static string Field(string name)
        {
            if (!s_camelCase || char.IsLower(name[0]))
                return name;

            return char.ToLowerInvariant(name[0]) + name[1..];
        }

        public static class Finalisation
        {
            public static readonly string Timestamp = Field(nameof(Entities.Finalisation.Timestamp));
            public static readonly string ReleaseType = Field(nameof(Entities.Finalisation.ReleaseType));
            public static readonly string Mrn = Field(nameof(Entities.Finalisation.Mrn));
        }

        public static class Decision
        {
            public static readonly string Timestamp = Field(nameof(Entities.Decision.Timestamp));
            public static readonly string Mrn = Field(nameof(Entities.Decision.Mrn));
            public static readonly string MrnCreated = Field(nameof(Entities.Decision.MrnCreated));
            public static readonly string Match = Field(nameof(Entities.Decision.Match));
        }
    }

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
                new BsonDocument(Fields.Finalisation.Timestamp, new BsonDocument { { "$gte", from }, { "$lt", to } })
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
                                { "sortBy", new BsonDocument(Fields.Finalisation.Timestamp, -1) },
                                {
                                    "output",
                                    new BsonDocument(
                                        Fields.Finalisation.ReleaseType,
                                        $"${Fields.Finalisation.ReleaseType}"
                                    )
                                },
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
                                        new BsonArray
                                        {
                                            $"$latest.{Fields.Finalisation.ReleaseType}",
                                            ReleaseType.Automatic,
                                        }
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
                                        new BsonArray
                                        {
                                            $"$latest.{Fields.Finalisation.ReleaseType}",
                                            ReleaseType.Manual,
                                        }
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
                new BsonDocument(Fields.Finalisation.Timestamp, new BsonDocument { { "$gte", from }, { "$lt", to } })
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
                                { "date", $"${Fields.Finalisation.Timestamp}" },
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
                        new BsonDocument
                        {
                            { "bucket", "$bucket" },
                            { Fields.Finalisation.Mrn, $"${Fields.Finalisation.Mrn}" },
                        }
                    },
                    {
                        "latest",
                        new BsonDocument(
                            "$top",
                            new BsonDocument
                            {
                                { "sortBy", new BsonDocument(Fields.Finalisation.Timestamp, -1) },
                                {
                                    "output",
                                    new BsonDocument(
                                        Fields.Finalisation.ReleaseType,
                                        $"${Fields.Finalisation.ReleaseType}"
                                    )
                                },
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
                                        new BsonArray
                                        {
                                            $"$latest.{Fields.Finalisation.ReleaseType}",
                                            ReleaseType.Automatic,
                                        }
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
                                        new BsonArray
                                        {
                                            $"$latest.{Fields.Finalisation.ReleaseType}",
                                            ReleaseType.Manual,
                                        }
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

    public async Task<MatchesSummary> GetMatchesSummary(DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        var aggregatePipeline = new[]
        {
            new BsonDocument(
                "$match",
                new BsonDocument(Fields.Decision.MrnCreated, new BsonDocument { { "$gte", from }, { "$lt", to } })
            ),
            new BsonDocument(
                "$group",
                new BsonDocument
                {
                    { "_id", $"${Fields.Decision.Mrn}" },
                    {
                        "latest",
                        new BsonDocument(
                            "$top",
                            new BsonDocument
                            {
                                { "sortBy", new BsonDocument(Fields.Decision.Timestamp, -1) },
                                { "output", new BsonDocument(Fields.Decision.Match, $"${Fields.Decision.Match}") },
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
                        "match",
                        new BsonDocument(
                            "$sum",
                            new BsonDocument(
                                "$cond",
                                new BsonArray
                                {
                                    new BsonDocument("$eq", new BsonArray { $"$latest.{Fields.Decision.Match}", true }),
                                    1,
                                    0,
                                }
                            )
                        )
                    },
                    {
                        "noMatch",
                        new BsonDocument(
                            "$sum",
                            new BsonDocument(
                                "$cond",
                                new BsonArray
                                {
                                    new BsonDocument(
                                        "$eq",
                                        new BsonArray { $"$latest.{Fields.Decision.Match}", false }
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
                    { "match", 1 },
                    { "noMatch", 1 },
                    { "total", 1 },
                }
            ),
        };

        var aggregateTask = dbContext.Decisions.AggregateAsync<MatchesSummary>(
            aggregatePipeline,
            cancellationToken: cancellationToken
        );

        var results = await (await aggregateTask).ToListAsync(cancellationToken);

        return results.FirstOrDefault() ?? new MatchesSummary(0, 0, 0);
    }

    public async Task<IReadOnlyList<MatchesBucket>> GetMatchesBuckets(
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
                new BsonDocument(Fields.Decision.MrnCreated, new BsonDocument { { "$gte", from }, { "$lt", to } })
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
                                { "date", $"${Fields.Decision.MrnCreated}" },
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
                        new BsonDocument { { "bucket", "$bucket" }, { Fields.Decision.Mrn, $"${Fields.Decision.Mrn}" } }
                    },
                    {
                        "latest",
                        new BsonDocument(
                            "$top",
                            new BsonDocument
                            {
                                { "sortBy", new BsonDocument(Fields.Decision.Timestamp, -1) },
                                { "output", new BsonDocument(Fields.Decision.Match, $"${Fields.Decision.Match}") },
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
                        "match",
                        new BsonDocument(
                            "$sum",
                            new BsonDocument(
                                "$cond",
                                new BsonArray
                                {
                                    new BsonDocument("$eq", new BsonArray { $"$latest.{Fields.Decision.Match}", true }),
                                    1,
                                    0,
                                }
                            )
                        )
                    },
                    {
                        "noMatch",
                        new BsonDocument(
                            "$sum",
                            new BsonDocument(
                                "$cond",
                                new BsonArray
                                {
                                    new BsonDocument(
                                        "$eq",
                                        new BsonArray { $"$latest.{Fields.Decision.Match}", false }
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
                            { "match", "$match" },
                            { "noMatch", "$noMatch" },
                            { "total", "$total" },
                        }
                    },
                }
            ),
            new BsonDocument("$sort", new BsonDocument { { "bucket", 1 } }),
        };

        var aggregateTask = dbContext.Decisions.AggregateAsync<MatchesBucket>(
            aggregatePipeline,
            cancellationToken: cancellationToken
        );

        return await (await aggregateTask).ToListAsync(cancellationToken);
    }
}
