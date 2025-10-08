using System.Diagnostics.CodeAnalysis;
using Defra.TradeImportsReportingApi.Api.Data.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

// ReSharper disable InconsistentNaming

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

    private static class Units
    {
        private const string Hour = "hour";
        private const string Day = "day";

        private static readonly IReadOnlyList<string> s_all = [Hour, Day];

        public static bool IsSupported(string unit) => s_all.Contains(unit);

        public static bool IsUtc(DateTime value) => value.Kind == DateTimeKind.Utc;

        public static DateTime GetBucketStart(DateTime from, string unit) =>
            unit switch
            {
                Hour => new DateTime(from.Year, from.Month, from.Day, from.Hour, 0, 0, from.Kind),
                Day => new DateTime(from.Year, from.Month, from.Day, 0, 0, 0, from.Kind),
                _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unexpected unit"),
            };

        public static DateTime GetNextBucket(DateTime bucket, string unit) =>
            unit switch
            {
                Hour => bucket.AddHours(1),
                Day => bucket.AddDays(1),
                _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unexpected unit"),
            };
    }

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

        public static class Request
        {
            public static readonly string Timestamp = Field(nameof(Entities.Request.Timestamp));
            public static readonly string Mrn = Field(nameof(Entities.Request.Mrn));
        }

        public static class Notification
        {
            public static readonly string Timestamp = Field(nameof(Entities.Notification.Timestamp));
            public static readonly string ReferenceNumber = Field(nameof(Entities.Notification.ReferenceNumber));
            public static readonly string NotificationCreated = Field(
                nameof(Entities.Notification.NotificationCreated)
            );
            public static readonly string NotificationType = Field(nameof(Entities.Notification.NotificationType));
        }
    }

    public async Task<ReleasesSummary> GetReleasesSummary(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken
    )
    {
        GuardUtc(from, to);

        const string automatic = nameof(automatic);
        const string manual = nameof(manual);
        const string total = nameof(total);

        var pipeline = new[]
        {
            ReleasesMatch(from, to),
            new BsonDocument(
                "$group",
                new BsonDocument
                {
                    { "_id", $"${Fields.Finalisation.Mrn}" },
                    SortAndTakeLatest(Fields.Finalisation.Timestamp, Fields.Finalisation.ReleaseType),
                }
            ),
            new BsonDocument(
                "$group",
                new BsonDocument
                {
                    { "_id", 1 },
                    FieldSum(automatic, Fields.Finalisation.ReleaseType, ReleaseType.Automatic),
                    FieldSum(manual, Fields.Finalisation.ReleaseType, ReleaseType.Manual),
                    { total, new BsonDocument("$sum", 1) },
                }
            ),
            new BsonDocument(
                "$project",
                new BsonDocument
                {
                    { "_id", 0 },
                    { automatic, 1 },
                    { manual, 1 },
                    { total, 1 },
                }
            ),
        };

        var aggregateTask = dbContext.Finalisations.AggregateAsync<ReleasesSummary>(
            pipeline,
            cancellationToken: cancellationToken
        );

        var results = await (await aggregateTask).ToListAsync(cancellationToken);

        return results.FirstOrDefault() ?? ReleasesSummary.Empty;
    }

    public async Task<IReadOnlyList<ReleasesBucket>> GetReleasesBuckets(
        DateTime from,
        DateTime to,
        string unit,
        CancellationToken cancellationToken
    )
    {
        GuardUtc(from, to);
        GuardUnit(unit);

        const string automatic = nameof(automatic);
        const string manual = nameof(manual);
        const string total = nameof(total);

        var pipeline = new[]
        {
            ReleasesMatch(from, to),
            new BsonDocument("$set", Bucket(Fields.Finalisation.Timestamp, unit)),
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
                    SortAndTakeLatest(Fields.Finalisation.Timestamp, Fields.Finalisation.ReleaseType),
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
                    FieldSum(automatic, Fields.Finalisation.ReleaseType, ReleaseType.Automatic),
                    FieldSum(manual, Fields.Finalisation.ReleaseType, ReleaseType.Manual),
                    { total, new BsonDocument("$sum", 1) },
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
                            { automatic, $"${automatic}" },
                            { manual, $"${manual}" },
                            { total, $"${total}" },
                        }
                    },
                }
            ),
            new BsonDocument("$sort", new BsonDocument { { "bucket", 1 } }),
        };

        var aggregateTask = dbContext.Finalisations.AggregateAsync<ReleasesBucket>(
            pipeline,
            cancellationToken: cancellationToken
        );

        var results = await (await aggregateTask).ToListAsync(cancellationToken) ?? [];

        return AddEmptyBuckets(from, to, unit, results, x => new ReleasesBucket(x, ReleasesSummary.Empty));
    }

    public async Task<IReadOnlyList<ReleasesBucket>> GetReleasesIntervals(
        DateTime from,
        DateTime to,
        DateTime[] intervals,
        CancellationToken cancellationToken
    )
    {
        GuardUtc(from, to, intervals);
        GuardIntervals(from, to, intervals);

        const string automatic = nameof(automatic);
        const string manual = nameof(manual);
        const string total = nameof(total);

        var boundaries = new[] { from }.Concat(intervals).Concat([to]).OrderBy(x => x).ToHashSet();
        var pipeline = new[]
        {
            ReleasesMatch(from, to),
            new BsonDocument(
                "$project",
                new BsonDocument
                {
                    { "_id", 0 },
                    { Fields.Finalisation.Mrn, 1 },
                    { Fields.Finalisation.Timestamp, 1 },
                    { Fields.Finalisation.ReleaseType, 1 },
                }
            ),
            new BsonDocument(
                "$set",
                new BsonDocument("boundaries", new BsonArray(boundaries.Select(x => (BsonValue)x)))
            ),
            new BsonDocument(
                "$set",
                new BsonDocument(
                    "bucket",
                    new BsonDocument(
                        "$let",
                        new BsonDocument
                        {
                            {
                                "vars",
                                new BsonDocument(
                                    "le",
                                    new BsonDocument(
                                        "$filter",
                                        new BsonDocument
                                        {
                                            { "input", "$boundaries" },
                                            { "as", "b" },
                                            {
                                                "cond",
                                                new BsonDocument(
                                                    "$lte",
                                                    new BsonArray { "$$b", $"${Fields.Finalisation.Timestamp}" }
                                                )
                                            },
                                        }
                                    )
                                )
                            },
                            {
                                "in",
                                new BsonDocument(
                                    "$arrayElemAt",
                                    new BsonArray
                                    {
                                        "$$le",
                                        new BsonDocument(
                                            "$subtract",
                                            new BsonArray { new BsonDocument("$size", "$$le"), 1 }
                                        ),
                                    }
                                )
                            },
                        }
                    )
                )
            ),
            new BsonDocument("$unset", "boundaries"),
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
                    SortAndTakeLatest(Fields.Finalisation.Timestamp, Fields.Finalisation.ReleaseType),
                }
            ),
            new BsonDocument(
                "$group",
                new BsonDocument
                {
                    { "_id", new BsonDocument("bucket", "$_id.bucket") },
                    FieldSum(automatic, Fields.Finalisation.ReleaseType, ReleaseType.Automatic),
                    FieldSum(manual, Fields.Finalisation.ReleaseType, ReleaseType.Manual),
                    { total, new BsonDocument("$sum", 1) },
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
                            { automatic, $"${automatic}" },
                            { manual, $"${manual}" },
                            { total, $"${total}" },
                        }
                    },
                }
            ),
            new BsonDocument("$sort", new BsonDocument("bucket", 1)),
        };

        var aggregateTask = dbContext.Finalisations.AggregateAsync<ReleasesBucket>(
            pipeline,
            cancellationToken: cancellationToken
        );

        var results = await (await aggregateTask).ToListAsync(cancellationToken) ?? [];

        return AddEmptyIntervals(intervals, results, x => new ReleasesBucket(x, ReleasesSummary.Empty));
    }

    public async Task<IReadOnlyList<Finalisation>> GetReleases(
        DateTime from,
        DateTime to,
        string releaseType,
        CancellationToken cancellationToken
    )
    {
        GuardUtc(from, to);

        var pipeline = new[]
        {
            // Do not restrict release type as final match in pipeline will do this
            ReleasesMatch(from, to, restrictReleaseType: false),
            new BsonDocument("$sort", new BsonDocument(Fields.Finalisation.Timestamp, -1)),
            new BsonDocument(
                "$group",
                new BsonDocument
                {
                    { "_id", $"${Fields.Finalisation.Mrn}" },
                    { "latest", new BsonDocument("$first", "$$ROOT") },
                }
            ),
            new BsonDocument("$replaceRoot", new BsonDocument("newRoot", "$latest")),
            new BsonDocument("$match", new BsonDocument(Fields.Finalisation.ReleaseType, releaseType)),
            new BsonDocument("$sort", new BsonDocument(Fields.Finalisation.Timestamp, -1)),
        };

        var aggregateTask = dbContext.Finalisations.AggregateAsync<Finalisation>(
            pipeline,
            cancellationToken: cancellationToken
        );

        return await (await aggregateTask).ToListAsync(cancellationToken) ?? [];
    }

    public async Task<MatchesSummary> GetMatchesSummary(DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        GuardUtc(from, to);

        const string match = nameof(match);
        const string noMatch = nameof(noMatch);
        const string total = nameof(total);

        var pipeline = new[]
        {
            MatchesMatch(from, to),
            new BsonDocument(
                "$group",
                new BsonDocument
                {
                    { "_id", $"${Fields.Decision.Mrn}" },
                    SortAndTakeLatest(Fields.Decision.Timestamp, Fields.Decision.Match),
                }
            ),
            new BsonDocument(
                "$group",
                new BsonDocument
                {
                    { "_id", 1 },
                    FieldSum(match, Fields.Decision.Match, true),
                    FieldSum(noMatch, Fields.Decision.Match, false),
                    { total, new BsonDocument("$sum", 1) },
                }
            ),
            new BsonDocument(
                "$project",
                new BsonDocument
                {
                    { "_id", 0 },
                    { match, 1 },
                    { noMatch, 1 },
                    { total, 1 },
                }
            ),
        };

        var aggregateTask = dbContext.Decisions.AggregateAsync<MatchesSummary>(
            pipeline,
            cancellationToken: cancellationToken
        );

        var results = await (await aggregateTask).ToListAsync(cancellationToken);

        return results.FirstOrDefault() ?? MatchesSummary.Empty;
    }

    public async Task<IReadOnlyList<MatchesBucket>> GetMatchesBuckets(
        DateTime from,
        DateTime to,
        string unit,
        CancellationToken cancellationToken
    )
    {
        GuardUtc(from, to);
        GuardUnit(unit);

        const string match = nameof(match);
        const string noMatch = nameof(noMatch);
        const string total = nameof(total);

        var pipeline = new[]
        {
            MatchesMatch(from, to),
            new BsonDocument("$set", Bucket(Fields.Decision.MrnCreated, unit)),
            new BsonDocument(
                "$group",
                new BsonDocument
                {
                    {
                        "_id",
                        new BsonDocument { { "bucket", "$bucket" }, { Fields.Decision.Mrn, $"${Fields.Decision.Mrn}" } }
                    },
                    SortAndTakeLatest(Fields.Decision.Timestamp, Fields.Decision.Match),
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
                    FieldSum(match, Fields.Decision.Match, true),
                    FieldSum(noMatch, Fields.Decision.Match, false),
                    { total, new BsonDocument("$sum", 1) },
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
                            { match, $"${match}" },
                            { noMatch, $"${noMatch}" },
                            { total, $"${total}" },
                        }
                    },
                }
            ),
            new BsonDocument("$sort", new BsonDocument { { "bucket", 1 } }),
        };

        var aggregateTask = dbContext.Decisions.AggregateAsync<MatchesBucket>(
            pipeline,
            cancellationToken: cancellationToken
        );

        var results = await (await aggregateTask).ToListAsync(cancellationToken) ?? [];

        return AddEmptyBuckets(from, to, unit, results, x => new MatchesBucket(x, MatchesSummary.Empty));
    }

    public async Task<IReadOnlyList<MatchesBucket>> GetMatchesIntervals(
        DateTime from,
        DateTime to,
        DateTime[] intervals,
        CancellationToken cancellationToken
    )
    {
        GuardUtc(from, to, intervals);
        GuardIntervals(from, to, intervals);

        const string match = nameof(match);
        const string noMatch = nameof(noMatch);
        const string total = nameof(total);

        var boundaries = new[] { from }.Concat(intervals).Concat([to]).OrderBy(x => x).ToHashSet();
        var pipeline = new[]
        {
            MatchesMatch(from, to),
            new BsonDocument(
                "$project",
                new BsonDocument
                {
                    { "_id", 0 },
                    { Fields.Decision.Mrn, 1 },
                    { Fields.Decision.Timestamp, 1 },
                    { Fields.Decision.Match, 1 },
                    { Fields.Decision.MrnCreated, 1 },
                }
            ),
            new BsonDocument(
                "$set",
                new BsonDocument("boundaries", new BsonArray(boundaries.Select(x => (BsonValue)x)))
            ),
            new BsonDocument(
                "$set",
                new BsonDocument(
                    "bucket",
                    new BsonDocument(
                        "$let",
                        new BsonDocument
                        {
                            {
                                "vars",
                                new BsonDocument(
                                    "le",
                                    new BsonDocument(
                                        "$filter",
                                        new BsonDocument
                                        {
                                            { "input", "$boundaries" },
                                            { "as", "b" },
                                            {
                                                "cond",
                                                new BsonDocument(
                                                    "$lte",
                                                    new BsonArray { "$$b", $"${Fields.Decision.MrnCreated}" }
                                                )
                                            },
                                        }
                                    )
                                )
                            },
                            {
                                "in",
                                new BsonDocument(
                                    "$arrayElemAt",
                                    new BsonArray
                                    {
                                        "$$le",
                                        new BsonDocument(
                                            "$subtract",
                                            new BsonArray { new BsonDocument("$size", "$$le"), 1 }
                                        ),
                                    }
                                )
                            },
                        }
                    )
                )
            ),
            new BsonDocument("$unset", "boundaries"),
            new BsonDocument(
                "$group",
                new BsonDocument
                {
                    {
                        "_id",
                        new BsonDocument { { "bucket", "$bucket" }, { Fields.Decision.Mrn, $"${Fields.Decision.Mrn}" } }
                    },
                    SortAndTakeLatest(Fields.Decision.Timestamp, Fields.Decision.Match),
                }
            ),
            new BsonDocument(
                "$group",
                new BsonDocument
                {
                    { "_id", new BsonDocument("bucket", "$_id.bucket") },
                    FieldSum(match, Fields.Decision.Match, true),
                    FieldSum(noMatch, Fields.Decision.Match, false),
                    { total, new BsonDocument("$sum", 1) },
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
                            { match, $"${match}" },
                            { noMatch, $"${noMatch}" },
                            { total, $"${total}" },
                        }
                    },
                }
            ),
            new BsonDocument("$sort", new BsonDocument("bucket", 1)),
        };

        var aggregateTask = dbContext.Decisions.AggregateAsync<MatchesBucket>(
            pipeline,
            cancellationToken: cancellationToken
        );

        var results = await (await aggregateTask).ToListAsync(cancellationToken) ?? [];

        return AddEmptyIntervals(intervals, results, x => new MatchesBucket(x, MatchesSummary.Empty));
    }

    public async Task<IReadOnlyList<Decision>> GetMatches(
        DateTime from,
        DateTime to,
        bool match,
        CancellationToken cancellationToken
    )
    {
        GuardUtc(from, to);

        var pipeline = new[]
        {
            MatchesMatch(from, to),
            new BsonDocument("$sort", new BsonDocument(Fields.Decision.Timestamp, -1)),
            new BsonDocument(
                "$group",
                new BsonDocument
                {
                    { "_id", $"${Fields.Decision.Mrn}" },
                    { "latest", new BsonDocument("$first", "$$ROOT") },
                }
            ),
            new BsonDocument("$replaceRoot", new BsonDocument("newRoot", "$latest")),
            new BsonDocument("$match", new BsonDocument(Fields.Decision.Match, match)),
            new BsonDocument("$sort", new BsonDocument(Fields.Decision.Timestamp, -1)),
        };

        var aggregateTask = dbContext.Decisions.AggregateAsync<Decision>(
            pipeline,
            cancellationToken: cancellationToken
        );

        return await (await aggregateTask).ToListAsync(cancellationToken) ?? [];
    }

    public async Task<ClearanceRequestsSummary> GetClearanceRequestsSummary(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken
    )
    {
        GuardUtc(from, to);

        var uniquePipeline = new[]
        {
            ClearanceRequestMatch(from, to),
            new BsonDocument("$group", new BsonDocument("_id", $"${Fields.Request.Mrn}")),
            new BsonDocument("$count", "count"),
        };

        var uniqueTask = dbContext.Requests.AggregateAsync<BsonDocument>(
            uniquePipeline,
            cancellationToken: cancellationToken
        );

        var totalPipeline = new[] { ClearanceRequestMatch(from, to), new BsonDocument("$count", "count") };

        var totalTask = dbContext.Requests.AggregateAsync<BsonDocument>(
            totalPipeline,
            cancellationToken: cancellationToken
        );

        await Task.WhenAll(uniqueTask, totalTask);

        var unique = await (await uniqueTask).FirstOrDefaultAsync(cancellationToken);
        var total = await (await totalTask).FirstOrDefaultAsync(cancellationToken);

        return new ClearanceRequestsSummary((int)(unique?["count"] ?? 0), (int)(total?["count"] ?? 0));
    }

    public async Task<IReadOnlyList<ClearanceRequestsBucket>> GetClearanceRequestsBuckets(
        DateTime from,
        DateTime to,
        string unit,
        CancellationToken cancellationToken
    )
    {
        GuardUtc(from, to);
        GuardUnit(unit);

        // Can only return buckets for unique MRNs across the time period.
        // Cannot return total overall as MRN might appear in more than one timestamp.

        var pipeline = new[]
        {
            ClearanceRequestMatch(from, to),
            new BsonDocument(
                "$group",
                new BsonDocument
                {
                    { "_id", $"${Fields.Request.Mrn}" },
                    SortAndTakeLatest(Fields.Request.Timestamp, Fields.Request.Timestamp),
                }
            ),
            new BsonDocument("$set", Bucket(Fields.Request.Timestamp, unit, fieldPrefix: "latest.")),
            new BsonDocument(
                "$group",
                new BsonDocument
                {
                    {
                        "_id",
                        new BsonDocument { { "bucket", "$bucket" } }
                    },
                    { "unique", new BsonDocument("$sum", 1) },
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
                            { "unique", "$unique" },
                            { "total", new BsonDocument("$literal", -1) }, // Cannot return, see comment at start of method
                        }
                    },
                }
            ),
            new BsonDocument("$sort", new BsonDocument { { "bucket", 1 } }),
        };

        var aggregateTask = dbContext.Requests.AggregateAsync<ClearanceRequestsBucket>(
            pipeline,
            cancellationToken: cancellationToken
        );

        var results = await (await aggregateTask).ToListAsync(cancellationToken) ?? [];

        return AddEmptyBuckets(
            from,
            to,
            unit,
            results,
            x => new ClearanceRequestsBucket(x, ClearanceRequestsSummary.Empty)
        );
    }

    public async Task<IReadOnlyList<ClearanceRequestsBucket>> GetClearanceRequestsIntervals(
        DateTime from,
        DateTime to,
        DateTime[] intervals,
        CancellationToken cancellationToken
    )
    {
        GuardUtc(from, to, intervals);
        GuardIntervals(from, to, intervals);

        var boundaries = new[] { from }.Concat(intervals).Concat([to]).OrderBy(x => x).ToHashSet();
        var uniquePipeline = new[]
        {
            ClearanceRequestMatch(from, to),
            new BsonDocument(
                "$project",
                new BsonDocument
                {
                    { "_id", 0 },
                    { Fields.Request.Mrn, 1 },
                    { Fields.Request.Timestamp, 1 },
                }
            ),
            new BsonDocument(
                "$group",
                new BsonDocument
                {
                    { "_id", $"${Fields.Request.Mrn}" },
                    { "latestTs", new BsonDocument("$max", $"${Fields.Request.Timestamp}") },
                }
            ),
            new BsonDocument("$project", new BsonDocument { { "_id", 0 }, { Fields.Request.Timestamp, "$latestTs" } }),
            new BsonDocument(
                "$set",
                new BsonDocument("boundaries", new BsonArray(boundaries.Select(x => (BsonValue)x)))
            ),
            new BsonDocument(
                "$set",
                new BsonDocument(
                    "bucket",
                    new BsonDocument(
                        "$let",
                        new BsonDocument
                        {
                            {
                                "vars",
                                new BsonDocument(
                                    "le",
                                    new BsonDocument(
                                        "$filter",
                                        new BsonDocument
                                        {
                                            { "input", "$boundaries" },
                                            { "as", "b" },
                                            {
                                                "cond",
                                                new BsonDocument(
                                                    "$lte",
                                                    new BsonArray { "$$b", $"${Fields.Request.Timestamp}" }
                                                )
                                            },
                                        }
                                    )
                                )
                            },
                            {
                                "in",
                                new BsonDocument(
                                    "$arrayElemAt",
                                    new BsonArray
                                    {
                                        "$$le",
                                        new BsonDocument(
                                            "$subtract",
                                            new BsonArray { new BsonDocument("$size", "$$le"), 1 }
                                        ),
                                    }
                                )
                            },
                        }
                    )
                )
            ),
            new BsonDocument("$unset", "boundaries"),
            new BsonDocument(
                "$group",
                new BsonDocument
                {
                    { "_id", new BsonDocument("bucket", "$bucket") },
                    { "unique", new BsonDocument("$sum", 1) },
                }
            ),
            new BsonDocument(
                "$project",
                new BsonDocument
                {
                    { "_id", 0 },
                    { "bucket", "$_id.bucket" },
                    { "unique", 1 },
                }
            ),
            new BsonDocument("$sort", new BsonDocument("bucket", 1)),
        };

        var totalPipeline = new[]
        {
            ClearanceRequestMatch(from, to),
            new BsonDocument("$project", new BsonDocument { { "_id", 0 }, { Fields.Request.Timestamp, 1 } }),
            new BsonDocument(
                "$set",
                new BsonDocument("boundaries", new BsonArray(boundaries.Select(x => (BsonValue)x)))
            ),
            new BsonDocument(
                "$set",
                new BsonDocument(
                    "bucket",
                    new BsonDocument(
                        "$let",
                        new BsonDocument
                        {
                            {
                                "vars",
                                new BsonDocument(
                                    "le",
                                    new BsonDocument(
                                        "$filter",
                                        new BsonDocument
                                        {
                                            { "input", "$boundaries" },
                                            { "as", "b" },
                                            {
                                                "cond",
                                                new BsonDocument(
                                                    "$lte",
                                                    new BsonArray { "$$b", $"${Fields.Request.Timestamp}" }
                                                )
                                            },
                                        }
                                    )
                                )
                            },
                            {
                                "in",
                                new BsonDocument(
                                    "$arrayElemAt",
                                    new BsonArray
                                    {
                                        "$$le",
                                        new BsonDocument(
                                            "$subtract",
                                            new BsonArray { new BsonDocument("$size", "$$le"), 1 }
                                        ),
                                    }
                                )
                            },
                        }
                    )
                )
            ),
            new BsonDocument("$unset", "boundaries"),
            new BsonDocument(
                "$group",
                new BsonDocument
                {
                    { "_id", new BsonDocument("bucket", "$bucket") },
                    { "total", new BsonDocument("$sum", 1) },
                }
            ),
            new BsonDocument(
                "$project",
                new BsonDocument
                {
                    { "_id", 0 },
                    { "bucket", "$_id.bucket" },
                    { "total", 1 },
                }
            ),
            new BsonDocument("$sort", new BsonDocument("bucket", 1)),
        };

        var uniqueTask = dbContext.Requests.AggregateAsync<BsonDocument>(
            uniquePipeline,
            cancellationToken: cancellationToken
        );
        var totalTask = dbContext.Requests.AggregateAsync<BsonDocument>(
            totalPipeline,
            cancellationToken: cancellationToken
        );

        await Task.WhenAll(uniqueTask, totalTask);

        var uniques = await (await uniqueTask).ToListAsync(cancellationToken);
        var totals = await (await totalTask).ToListAsync(cancellationToken);

        var uniqueByBucket = uniques.ToDictionary(x => x["bucket"].ToUniversalTime(), x => (int)x["unique"]);
        var totalByBucket = totals.ToDictionary(x => x["bucket"].ToUniversalTime(), x => (int)x["total"]);

        var allBuckets = uniqueByBucket.Keys.Union(totalByBucket.Keys).OrderBy(x => x);

        var results = allBuckets
            .Select(x => new ClearanceRequestsBucket(
                x,
                new ClearanceRequestsSummary(
                    uniqueByBucket.GetValueOrDefault(x, 0),
                    totalByBucket.GetValueOrDefault(x, 0)
                )
            ))
            .ToList();

        return AddEmptyIntervals(
            intervals,
            results,
            x => new ClearanceRequestsBucket(x, ClearanceRequestsSummary.Empty)
        );
    }

    public async Task<NotificationsSummary> GetNotificationsSummary(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken
    )
    {
        GuardUtc(from, to);

        const string chedA = nameof(chedA);
        const string chedP = nameof(chedP);
        const string chedPP = nameof(chedPP);
        const string chedD = nameof(chedD);
        const string total = nameof(total);

        var pipeline = new[]
        {
            NotificationsMatch(from, to),
            new BsonDocument(
                "$group",
                new BsonDocument
                {
                    { "_id", $"${Fields.Notification.ReferenceNumber}" },
                    SortAndTakeLatest(Fields.Notification.Timestamp, Fields.Notification.NotificationType),
                }
            ),
            new BsonDocument(
                "$group",
                new BsonDocument
                {
                    { "_id", 1 },
                    FieldSum(chedA, Fields.Notification.NotificationType, NotificationType.ChedA),
                    FieldSum(chedP, Fields.Notification.NotificationType, NotificationType.ChedP),
                    FieldSum(chedPP, Fields.Notification.NotificationType, NotificationType.ChedPP),
                    FieldSum(chedD, Fields.Notification.NotificationType, NotificationType.ChedD),
                    { total, new BsonDocument("$sum", 1) },
                }
            ),
            new BsonDocument(
                "$project",
                new BsonDocument
                {
                    { "_id", 0 },
                    { chedA, 1 },
                    { chedP, 1 },
                    { chedPP, 1 },
                    { chedD, 1 },
                    { total, 1 },
                }
            ),
        };

        var aggregateTask = dbContext.Notifications.AggregateAsync<NotificationsSummary>(
            pipeline,
            cancellationToken: cancellationToken
        );

        var results = await (await aggregateTask).ToListAsync(cancellationToken);

        return results.FirstOrDefault() ?? NotificationsSummary.Empty;
    }

    public async Task<IReadOnlyList<NotificationsBucket>> GetNotificationsBuckets(
        DateTime from,
        DateTime to,
        string unit,
        CancellationToken cancellationToken
    )
    {
        GuardUtc(from, to);
        GuardUnit(unit);

        const string chedA = nameof(chedA);
        const string chedP = nameof(chedP);
        const string chedPP = nameof(chedPP);
        const string chedD = nameof(chedD);
        const string total = nameof(total);

        var pipeline = new[]
        {
            NotificationsMatch(from, to),
            new BsonDocument("$set", Bucket(Fields.Notification.NotificationCreated, unit)),
            new BsonDocument(
                "$group",
                new BsonDocument
                {
                    {
                        "_id",
                        new BsonDocument
                        {
                            { "bucket", "$bucket" },
                            { Fields.Notification.ReferenceNumber, $"${Fields.Notification.ReferenceNumber}" },
                        }
                    },
                    SortAndTakeLatest(Fields.Notification.Timestamp, Fields.Notification.NotificationType),
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
                    FieldSum(chedA, Fields.Notification.NotificationType, NotificationType.ChedA),
                    FieldSum(chedP, Fields.Notification.NotificationType, NotificationType.ChedP),
                    FieldSum(chedPP, Fields.Notification.NotificationType, NotificationType.ChedPP),
                    FieldSum(chedD, Fields.Notification.NotificationType, NotificationType.ChedD),
                    { total, new BsonDocument("$sum", 1) },
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
                            { chedA, $"${chedA}" },
                            { chedP, $"${chedP}" },
                            { chedPP, $"${chedPP}" },
                            { chedD, $"${chedD}" },
                            { total, $"${total}" },
                        }
                    },
                }
            ),
            new BsonDocument("$sort", new BsonDocument { { "bucket", 1 } }),
        };

        var aggregateTask = dbContext.Notifications.AggregateAsync<NotificationsBucket>(
            pipeline,
            cancellationToken: cancellationToken
        );

        var results = await (await aggregateTask).ToListAsync(cancellationToken) ?? [];

        return AddEmptyBuckets(from, to, unit, results, x => new NotificationsBucket(x, NotificationsSummary.Empty));
    }

    public async Task<IReadOnlyList<NotificationsBucket>> GetNotificationsIntervals(
        DateTime from,
        DateTime to,
        DateTime[] intervals,
        CancellationToken cancellationToken
    )
    {
        GuardUtc(from, to, intervals);
        GuardIntervals(from, to, intervals);

        const string chedA = nameof(chedA);
        const string chedP = nameof(chedP);
        const string chedPP = nameof(chedPP);
        const string chedD = nameof(chedD);
        const string total = nameof(total);

        var boundaries = new[] { from }.Concat(intervals).Concat([to]).OrderBy(x => x).ToHashSet();
        var pipeline = new[]
        {
            NotificationsMatch(from, to),
            new BsonDocument(
                "$project",
                new BsonDocument
                {
                    { "_id", 0 },
                    { Fields.Notification.ReferenceNumber, 1 },
                    { Fields.Notification.Timestamp, 1 },
                    { Fields.Notification.NotificationType, 1 },
                    { Fields.Notification.NotificationCreated, 1 },
                }
            ),
            new BsonDocument(
                "$group",
                new BsonDocument
                {
                    { "_id", $"${Fields.Notification.ReferenceNumber}" },
                    {
                        "latest",
                        new BsonDocument(
                            "$top",
                            new BsonDocument
                            {
                                { "sortBy", new BsonDocument(Fields.Notification.Timestamp, -1) },
                                { "output", "$$ROOT" },
                            }
                        )
                    },
                }
            ),
            new BsonDocument(
                "$set",
                new BsonDocument("boundaries", new BsonArray(boundaries.Select(x => (BsonValue)x)))
            ),
            new BsonDocument(
                "$set",
                new BsonDocument(
                    "bucket",
                    new BsonDocument(
                        "$let",
                        new BsonDocument
                        {
                            {
                                "vars",
                                new BsonDocument(
                                    "le",
                                    new BsonDocument(
                                        "$filter",
                                        new BsonDocument
                                        {
                                            { "input", "$boundaries" },
                                            { "as", "b" },
                                            {
                                                "cond",
                                                new BsonDocument(
                                                    "$lte",
                                                    new BsonArray
                                                    {
                                                        "$$b",
                                                        $"$latest.{Fields.Notification.NotificationCreated}",
                                                    }
                                                )
                                            },
                                        }
                                    )
                                )
                            },
                            {
                                "in",
                                new BsonDocument(
                                    "$arrayElemAt",
                                    new BsonArray
                                    {
                                        "$$le",
                                        new BsonDocument(
                                            "$subtract",
                                            new BsonArray { new BsonDocument("$size", "$$le"), 1 }
                                        ),
                                    }
                                )
                            },
                        }
                    )
                )
            ),
            new BsonDocument("$unset", "boundaries"),
            new BsonDocument(
                "$group",
                new BsonDocument
                {
                    { "_id", new BsonDocument("bucket", "$bucket") },
                    FieldSum(chedA, Fields.Notification.NotificationType, NotificationType.ChedA),
                    FieldSum(chedP, Fields.Notification.NotificationType, NotificationType.ChedP),
                    FieldSum(chedPP, Fields.Notification.NotificationType, NotificationType.ChedPP),
                    FieldSum(chedD, Fields.Notification.NotificationType, NotificationType.ChedD),
                    { total, new BsonDocument("$sum", 1) },
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
                            { chedA, $"${chedA}" },
                            { chedP, $"${chedP}" },
                            { chedPP, $"${chedPP}" },
                            { chedD, $"${chedD}" },
                            { total, $"${total}" },
                        }
                    },
                }
            ),
            new BsonDocument("$sort", new BsonDocument("bucket", 1)),
        };

        var aggregateTask = dbContext.Notifications.AggregateAsync<NotificationsBucket>(
            pipeline,
            cancellationToken: cancellationToken
        );

        var results = await (await aggregateTask).ToListAsync(cancellationToken) ?? [];

        return AddEmptyIntervals(intervals, results, x => new NotificationsBucket(x, NotificationsSummary.Empty));
    }

    public async Task<LastReceivedSummary> GetLastReceivedSummary(CancellationToken cancellationToken)
    {
        var finalisationTask = dbContext
            .Finalisations.Find(FilterDefinition<Finalisation>.Empty)
            .SortByDescending(x => x.Timestamp)
            .Project(x => new LastReceived(x.Timestamp, x.Mrn))
            .FirstOrDefaultAsync(cancellationToken);

        var requestTask = dbContext
            .Requests.Find(FilterDefinition<Request>.Empty)
            .SortByDescending(x => x.Timestamp)
            .Project(x => new LastReceived(x.Timestamp, x.Mrn))
            .FirstOrDefaultAsync(cancellationToken);

        var notificationTask = dbContext
            .Notifications.Find(FilterDefinition<Notification>.Empty)
            .SortByDescending(x => x.Timestamp)
            .Project(x => new LastReceived(x.Timestamp, x.ReferenceNumber))
            .FirstOrDefaultAsync(cancellationToken);

        await Task.WhenAll(finalisationTask, requestTask, notificationTask);

        var latestFinalisation = await finalisationTask;
        var latestRequest = await requestTask;
        var latestNotification = await notificationTask;

        return new LastReceivedSummary(latestFinalisation, latestRequest, latestNotification);
    }

    public async Task<LastSentSummary> GetLastSentSummary(CancellationToken cancellationToken)
    {
        var decision = await dbContext
            .Decisions.Find(FilterDefinition<Decision>.Empty)
            .SortByDescending(x => x.Timestamp)
            .Project(x => new LastSent(x.Timestamp, x.Mrn))
            .FirstOrDefaultAsync(cancellationToken);

        return new LastSentSummary(decision);
    }

    private static List<T> AddEmptyBuckets<T>(
        DateTime from,
        DateTime to,
        string unit,
        List<T> results,
        Func<DateTime, T> emptyBucketFunc
    )
        where T : IBucket
    {
        var bucketStart = Units.GetBucketStart(from, unit);
        var resultsByBucket = results.ToDictionary(x => x.Bucket, x => x);

        for (var bucket = bucketStart; bucket <= to; bucket = Units.GetNextBucket(bucket, unit))
        {
            if (!resultsByBucket.ContainsKey(bucket))
            {
                resultsByBucket.Add(bucket, emptyBucketFunc(bucket));
            }
        }

        return resultsByBucket.Values.OrderBy(x => x.Bucket).ToList();
    }

    private static List<T> AddEmptyIntervals<T>(
        DateTime[] intervals,
        List<T> results,
        Func<DateTime, T> emptyBucketFunc
    )
        where T : IBucket
    {
        var resultsByBucket = results.ToDictionary(x => x.Bucket, x => x);

        foreach (var interval in intervals.Where(x => !resultsByBucket.ContainsKey(x)))
        {
            resultsByBucket.Add(interval, emptyBucketFunc(interval));
        }

        return resultsByBucket.Values.OrderBy(x => x.Bucket).ToList();
    }

    private static void GuardUtc(DateTime from, DateTime to, DateTime[]? intervals = null)
    {
        if (!Units.IsUtc(from))
            throw new ArgumentOutOfRangeException(nameof(from), from, "From must be UTC");

        if (!Units.IsUtc(to))
            throw new ArgumentOutOfRangeException(nameof(to), to, "To must be UTC");

        if (intervals != null && intervals.Any(interval => !Units.IsUtc(interval)))
            throw new ArgumentOutOfRangeException(nameof(intervals), intervals, "Intervals must be UTC");
    }

    private static void GuardUnit(string unit)
    {
        if (!Units.IsSupported(unit))
            throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unexpected unit");
    }

    private static void GuardIntervals(DateTime from, DateTime to, DateTime[] intervals)
    {
        if (intervals.Any(x => x < from))
            throw new ArgumentOutOfRangeException(nameof(intervals), intervals, "Intervals must be after from");

        if (intervals.Any(x => x > to))
            throw new ArgumentOutOfRangeException(nameof(intervals), intervals, "Intervals must be before to");
    }

    private static BsonDocument ReleasesMatch(DateTime from, DateTime to, bool restrictReleaseType = true)
    {
        var match = FromAndToMatch(Fields.Finalisation.Timestamp, from, to);

        if (restrictReleaseType)
            match.Add(
                Fields.Finalisation.ReleaseType,
                new BsonDocument("$in", new BsonArray { ReleaseType.Automatic, ReleaseType.Manual })
            );

        return new BsonDocument("$match", match);
    }

    private static BsonDocument MatchesMatch(DateTime from, DateTime to) =>
        new("$match", FromAndToMatch(Fields.Decision.MrnCreated, from, to));

    private static BsonDocument ClearanceRequestMatch(DateTime from, DateTime to) =>
        new("$match", FromAndToMatch(Fields.Request.Timestamp, from, to));

    private static BsonDocument NotificationsMatch(DateTime from, DateTime to) =>
        new("$match", FromAndToMatch(Fields.Notification.NotificationCreated, from, to));

    private static BsonDocument FromAndToMatch(string field, DateTime from, DateTime to) =>
        new(field, new BsonDocument { { "$gte", from }, { "$lt", to } });

    private static BsonElement FieldSum(string name, string field, BsonValue value)
    {
        return new BsonElement(
            name,
            new BsonDocument(
                "$sum",
                new BsonDocument(
                    "$cond",
                    new BsonArray { new BsonDocument("$eq", new BsonArray { $"$latest.{field}", value }), 1, 0 }
                )
            )
        );
    }

    private static BsonElement SortAndTakeLatest(string sortByField, string returnField)
    {
        return new BsonElement(
            "latest",
            new BsonDocument(
                "$top",
                new BsonDocument
                {
                    { "sortBy", new BsonDocument(sortByField, -1) },
                    { "output", new BsonDocument(returnField, $"${returnField}") },
                }
            )
        );
    }

    private static BsonDocument Bucket(string field, string unit, string? fieldPrefix = null)
    {
        return new BsonDocument
        {
            {
                "bucket",
                new BsonDocument(
                    "$dateTrunc",
                    new BsonDocument
                    {
                        { "date", $"${fieldPrefix}{field}" },
                        { "unit", unit },
                        { "timezone", "UTC" },
                    }
                )
            },
        };
    }
}
