using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using MongoDB.Driver.Authentication.AWS;
using MongoDB.Driver.Core.Events;

namespace Defra.TradeImportsReportingApi.Api.Data.Extensions;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddDbContext(
        this IServiceCollection services,
        IConfiguration configuration,
        bool integrationTest
    )
    {
        services
            .AddOptions<MongoDbOptions>()
            .Bind(configuration.GetSection(MongoDbOptions.SectionName))
            .ValidateDataAnnotations();

        if (integrationTest)
            return services;

        services.AddHostedService<MongoIndexService>();
        services.AddScoped<IDbContext, MongoDbContext>();
        services.AddSingleton<MongoCommandTracker>();
        services.AddSingleton(sp =>
        {
            MongoClientSettings.Extensions.AddAWSAuthentication();

            var options =
                sp.GetService<IOptions<MongoDbOptions>>() ?? throw new InvalidOperationException("Options not found");
            var settings = MongoClientSettings.FromConnectionString(options.Value.DatabaseUri);

            if (options.Value.QueryLogging)
            {
                var commandTracker = sp.GetRequiredService<MongoCommandTracker>();

                settings.ClusterConfigurator = cb =>
                {
                    cb.Subscribe<CommandStartedEvent>(commandTracker.OnCommandStarted);
                    cb.Subscribe<CommandSucceededEvent>(commandTracker.OnCommandSucceeded);
                    cb.Subscribe<CommandFailedEvent>(commandTracker.OnCommandFailed);
                };
            }

            var client = new MongoClient(settings);
            var conventionPack = new ConventionPack
            {
                new CamelCaseElementNameConvention(),
                new EnumRepresentationConvention(BsonType.String),
            };

            ConventionRegistry.Register(nameof(conventionPack), conventionPack, _ => true);

            return client.GetDatabase(options.Value.DatabaseName);
        });

        return services;
    }

    private sealed partial class MongoCommandTracker(ILogger<MongoCommandTracker> logger)
    {
        private readonly ConcurrentDictionary<long, Command> _commands = new();

        private sealed record Command(string CommandName, BsonDocument Raw, long Timestamp);

        private static readonly JsonWriterSettings s_relaxed = new()
        {
            OutputMode = JsonOutputMode.RelaxedExtendedJson,
            Indent = false,
        };

        private static string ToIsoDateJson(BsonDocument doc)
        {
            var json = doc.ToJson(s_relaxed);

            return FindDollarDate().Replace(json, "ISODate(\"$1\")");
        }

        private static string RenderQueryAsExplain(BsonDocument document)
        {
            if (document.Contains("aggregate"))
                return RenderAggregateQuery(document);

            if (document.Contains("find"))
                return RenderFindQuery(document);

            if (document.Contains("count"))
                return RenderCountQuery(document);

            return document.Contains("distinct")
                ? RenderDistinctQuery(document)
                : "// unsupported command for shell explain";
        }

        private static string RenderDistinctQuery(BsonDocument document)
        {
            var key = document.GetValue("key", "").ToString();
            var query = document.GetValue("query", new BsonDocument()).AsBsonDocument;

            return $"db.getCollection(\"{document["distinct"].AsString}\").distinct(\"{key}\", {ToIsoDateJson(query)}).explain(\"executionStats\")";
        }

        private static string RenderCountQuery(BsonDocument document)
        {
            var query = document.GetValue("query", new BsonDocument()).AsBsonDocument;

            return $"db.getCollection(\"{document["count"].AsString}\").count({ToIsoDateJson(query)}).explain(\"executionStats\")";
        }

        private static string RenderFindQuery(BsonDocument document)
        {
            var filter = document.GetValue("filter", new BsonDocument()).AsBsonDocument;
            var proj = document.Contains("projection") ? document["projection"].AsBsonDocument : null;
            var sort = document.Contains("sort") ? document["sort"].AsBsonDocument : null;

            var command = $"db.getCollection(\"{document["find"].AsString}\").find({ToIsoDateJson(filter)})";

            if (proj is not null)
                command += $".project({ToIsoDateJson(proj)})";

            if (sort is not null)
                command += $".sort({ToIsoDateJson(sort)})";

            return command + ".explain(\"executionStats\")";
        }

        private static string RenderAggregateQuery(BsonDocument document)
        {
            var pipeline = document.GetValue("pipeline", new BsonArray());
            var wrapped = new BsonDocument("p", pipeline);
            var json = ToIsoDateJson(wrapped);
            var i = json.IndexOf('[');
            var j = json.LastIndexOf(']');
            var aggregate = i >= 0 && j > i ? json.Substring(i, j - i + 1) : "[]";

            return $"db.getCollection(\"{document["aggregate"].AsString}\").aggregate({aggregate}).explain(\"executionStats\")";
        }

        public void OnCommandStarted(CommandStartedEvent @event)
        {
            if (@event.OperationId is not null && ShouldTrack(@event))
                _commands.TryAdd(
                    @event.OperationId.Value,
                    new Command(
                        @event.CommandName,
                        (BsonDocument)@event.Command.DeepClone(),
                        TimeProvider.System.GetTimestamp()
                    )
                );
        }

        public void OnCommandSucceeded(CommandSucceededEvent @event)
        {
            if (@event.OperationId is not null && _commands.TryRemove(@event.OperationId.Value, out var commandInfo))
                Log(LogLevel.Information, commandInfo);
        }

        public void OnCommandFailed(CommandFailedEvent @event)
        {
            if (@event.OperationId is not null && _commands.TryRemove(@event.OperationId.Value, out var commandInfo))
                Log(LogLevel.Warning, commandInfo);
        }

        private void Log(LogLevel level, Command command)
        {
            logger.Log(
                level,
                "Mongo query {Result} {CommandName} {Query} took {Duration}ms",
                level == LogLevel.Information ? "succeeded" : "failed",
                command.CommandName,
                RenderQueryAsExplain(command.Raw),
                TimeProvider.System.GetElapsedTime(command.Timestamp).TotalMilliseconds
            );
        }

        private static bool ShouldTrack(CommandStartedEvent @event) =>
            @event.CommandName is "find" or "aggregate" or "count" or "distinct";

        [GeneratedRegex("\\{\\s*\"\\$date\"\\s*:\\s*\"([^\"]+)\"\\s*\\}")]
        private static partial Regex FindDollarDate();
    }
}
