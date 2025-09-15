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

        var pipeline = new[]
        {
            new BsonDocument(
                "$match",
                new BsonDocument
                {
                    {
                        Fields.Finalisation.Timestamp,
                        new BsonDocument { { "$gte", from }, { "$lt", to } }
                    },
                    {
                        Fields.Finalisation.ReleaseType,
                        new BsonDocument("$in", new BsonArray { ReleaseType.Automatic, ReleaseType.Manual })
                    },
                }
            ),
            new BsonDocument(
                "$group",
                new BsonDocument
                {
                    { "_id", $"${Fields.Finalisation.Mrn}" },
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

        var pipeline = new[]
        {
            new BsonDocument(
                "$match",
                new BsonDocument
                {
                    {
                        Fields.Finalisation.Timestamp,
                        new BsonDocument { { "$gte", from }, { "$lt", to } }
                    },
                    {
                        Fields.Finalisation.ReleaseType,
                        new BsonDocument("$in", new BsonArray { ReleaseType.Automatic, ReleaseType.Manual })
                    },
                }
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
            pipeline,
            cancellationToken: cancellationToken
        );

        var results = await (await aggregateTask).ToListAsync(cancellationToken) ?? [];

        return AddEmptyBuckets(from, to, unit, results, x => new ReleasesBucket(x, ReleasesSummary.Empty));
    }

    public async Task<MatchesSummary> GetMatchesSummary(DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        GuardUtc(from, to);

        var pipeline = new[]
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

        var pipeline = new[]
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
            pipeline,
            cancellationToken: cancellationToken
        );

        var results = await (await aggregateTask).ToListAsync(cancellationToken) ?? [];

        return AddEmptyBuckets(from, to, unit, results, x => new MatchesBucket(x, MatchesSummary.Empty));
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
            new BsonDocument(
                "$match",
                new BsonDocument(Fields.Decision.MrnCreated, new BsonDocument { { "$gte", from }, { "$lt", to } })
            ),
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

        var totalTask = dbContext.Requests.CountDocumentsAsync(
            Builders<Request>.Filter.Gte(x => x.Timestamp, from) & Builders<Request>.Filter.Lt(x => x.Timestamp, to),
            cancellationToken: cancellationToken
        );

        var pipeline = new[]
        {
            new BsonDocument(
                "$match",
                new BsonDocument(Fields.Request.Timestamp, new BsonDocument { { "$gte", from }, { "$lt", to } })
            ),
            new BsonDocument("$group", new BsonDocument("_id", $"${Fields.Request.Mrn}")),
            new BsonDocument("$count", "count"),
        };

        var uniqueTask = dbContext.Requests.AggregateAsync<BsonDocument>(
            pipeline,
            cancellationToken: cancellationToken
        );

        await Task.WhenAll(totalTask, uniqueTask);

        var total = (int)await totalTask;
        var unique = await (await uniqueTask).FirstOrDefaultAsync(cancellationToken);

        return new ClearanceRequestsSummary((int)(unique?["count"] ?? 0), total);
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
            new BsonDocument(
                "$match",
                new BsonDocument(Fields.Request.Timestamp, new BsonDocument { { "$gte", from }, { "$lt", to } })
            ),
            new BsonDocument(
                "$group",
                new BsonDocument
                {
                    { "_id", $"${Fields.Request.Mrn}" },
                    {
                        "latest",
                        new BsonDocument(
                            "$top",
                            new BsonDocument
                            {
                                { "sortBy", new BsonDocument(Fields.Request.Timestamp, -1) },
                                { "output", new BsonDocument("ts", $"${Fields.Request.Timestamp}") },
                            }
                        )
                    },
                }
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
                                { "date", "$latest.ts" },
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

    public async Task<NotificationsSummary> GetNotificationsSummary(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken
    )
    {
        GuardUtc(from, to);

        var pipeline = new[]
        {
            new BsonDocument(
                "$match",
                new BsonDocument(
                    Fields.Notification.NotificationCreated,
                    new BsonDocument { { "$gte", from }, { "$lt", to } }
                )
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
                                {
                                    "output",
                                    new BsonDocument(
                                        Fields.Notification.NotificationType,
                                        $"${Fields.Notification.NotificationType}"
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
                        "chedA",
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
                                            $"$latest.{Fields.Notification.NotificationType}",
                                            NotificationType.ChedA,
                                        }
                                    ),
                                    1,
                                    0,
                                }
                            )
                        )
                    },
                    {
                        "chedP",
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
                                            $"$latest.{Fields.Notification.NotificationType}",
                                            NotificationType.ChedP,
                                        }
                                    ),
                                    1,
                                    0,
                                }
                            )
                        )
                    },
                    {
                        "chedPP",
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
                                            $"$latest.{Fields.Notification.NotificationType}",
                                            NotificationType.ChedPP,
                                        }
                                    ),
                                    1,
                                    0,
                                }
                            )
                        )
                    },
                    {
                        "chedD",
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
                                            $"$latest.{Fields.Notification.NotificationType}",
                                            NotificationType.ChedD,
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
                    { "chedA", 1 },
                    { "chedP", 1 },
                    { "chedPP", 1 },
                    { "chedD", 1 },
                    { "total", 1 },
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

        var pipeline = new[]
        {
            new BsonDocument(
                "$match",
                new BsonDocument(
                    Fields.Notification.NotificationCreated,
                    new BsonDocument { { "$gte", from }, { "$lt", to } }
                )
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
                                { "date", $"${Fields.Notification.NotificationCreated}" },
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
                            { Fields.Notification.ReferenceNumber, $"${Fields.Notification.ReferenceNumber}" },
                        }
                    },
                    {
                        "latest",
                        new BsonDocument(
                            "$top",
                            new BsonDocument
                            {
                                { "sortBy", new BsonDocument(Fields.Notification.Timestamp, -1) },
                                {
                                    "output",
                                    new BsonDocument(
                                        Fields.Notification.NotificationType,
                                        $"${Fields.Notification.NotificationType}"
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
                        "chedA",
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
                                            $"$latest.{Fields.Notification.NotificationType}",
                                            NotificationType.ChedA,
                                        }
                                    ),
                                    1,
                                    0,
                                }
                            )
                        )
                    },
                    {
                        "chedP",
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
                                            $"$latest.{Fields.Notification.NotificationType}",
                                            NotificationType.ChedP,
                                        }
                                    ),
                                    1,
                                    0,
                                }
                            )
                        )
                    },
                    {
                        "chedPP",
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
                                            $"$latest.{Fields.Notification.NotificationType}",
                                            NotificationType.ChedPP,
                                        }
                                    ),
                                    1,
                                    0,
                                }
                            )
                        )
                    },
                    {
                        "chedD",
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
                                            $"$latest.{Fields.Notification.NotificationType}",
                                            NotificationType.ChedD,
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
                            { "chedA", "$chedA" },
                            { "chedP", "$chedP" },
                            { "chedPP", "$chedPP" },
                            { "chedD", "$chedD" },
                            { "total", "$total" },
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

    public async Task<LastReceivedSummary> GetLastReceivedSummary(CancellationToken cancellationToken)
    {
        var latestFinalisation = await dbContext
            .Finalisations.Find(FilterDefinition<Finalisation>.Empty)
            .SortByDescending(x => x.Timestamp)
            .Project(x => new LastReceived(x.Timestamp, x.Mrn))
            .FirstOrDefaultAsync(cancellationToken);

        var latestRequest = await dbContext
            .Requests.Find(FilterDefinition<Request>.Empty)
            .SortByDescending(x => x.Timestamp)
            .Project(x => new LastReceived(x.Timestamp, x.Mrn))
            .FirstOrDefaultAsync(cancellationToken);

        return new LastReceivedSummary(latestFinalisation, latestRequest);
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

    private static void GuardUtc(DateTime from, DateTime to)
    {
        if (!Units.IsUtc(from))
            throw new ArgumentOutOfRangeException(nameof(from), from, "From must be UTC");

        if (!Units.IsUtc(to))
            throw new ArgumentOutOfRangeException(nameof(to), to, "To must be UTC");
    }

    private static void GuardUnit(string unit)
    {
        if (!Units.IsSupported(unit))
            throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unexpected unit");
    }
}
