using Defra.TradeImportsReportingApi.Api.Data;
using Microsoft.AspNetCore.Mvc;

namespace Defra.TradeImportsReportingApi.Api.Endpoints;

public static class EndpointRouteBuilderExtensions
{
    private const string GroupName = "Reporting";
    private const string Description = "Searchable period is the last 31 days";

    public static void MapEndpoints(this IEndpointRouteBuilder app)
    {
        MapReleasesEndpoints(app);
        MapMatchesEndpoints(app);
        MapClearanceRequestEndpoints(app);
        MapNotificationEndpoints(app);
        MapGeneralEndpoints(app);

        app.MapGet("last-received", LastReceived)
            .WithName("LastReceived")
            .WithTags(GroupName)
            .WithSummary("Get last received")
            .Produces<LastReceivedResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();
    }

    private static void MapGeneralEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("summary", Summary)
            .WithName("Summary")
            .WithTags(GroupName)
            .WithSummary("Get summary")
            .WithDescription(Description)
            .Produces<SummaryResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        app.MapGet("buckets", Buckets)
            .WithName("Buckets")
            .WithTags(GroupName)
            .WithSummary("Get buckets by day or hour")
            .WithDescription(Description)
            .Produces<BucketsResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();
    }

    private static void MapNotificationEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("notifications/summary", NotificationsSummary)
            .WithName("NotificationsSummary")
            .WithTags(GroupName)
            .WithSummary("Get notifications summary")
            .WithDescription(Description)
            .Produces<NotificationsSummaryResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        app.MapGet("notifications/buckets", NotificationsBuckets)
            .WithName("NotificationsBuckets")
            .WithTags(GroupName)
            .WithSummary("Get notifications buckets by day or hour")
            .WithDescription(Description)
            .Produces<BucketsResponse<BucketResponse<NotificationsSummaryResponse>>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();
    }

    private static void MapClearanceRequestEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("clearance-requests/summary", ClearanceRequestsSummary)
            .WithName("ClearanceRequestsSummary")
            .WithTags(GroupName)
            .WithSummary("Get clearance requests summary")
            .WithDescription(Description)
            .Produces<ClearanceRequestsSummaryResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        app.MapGet("clearance-requests/buckets", ClearanceRequestsBuckets)
            .WithName("ClearanceRequestsBuckets")
            .WithTags(GroupName)
            .WithSummary("Get clearance requests buckets by day or hour")
            .WithDescription(Description)
            .Produces<BucketsResponse<BucketResponse<ClearanceRequestsSummaryBucketResponse>>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();
    }

    private static void MapMatchesEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("matches/summary", MatchesSummary)
            .WithName("MatchesSummary")
            .WithTags(GroupName)
            .WithSummary("Get matches summary")
            .WithDescription(Description)
            .Produces<MatchesSummaryResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        app.MapGet("matches/buckets", MatchesBuckets)
            .WithName("MatchesBuckets")
            .WithTags(GroupName)
            .WithSummary("Get matches buckets by day or hour")
            .WithDescription(Description)
            .Produces<BucketsResponse<BucketResponse<MatchesSummaryResponse>>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();
    }

    private static void MapReleasesEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("releases/summary", ReleasesSummary)
            .WithName("ReleasesSummary")
            .WithTags(GroupName)
            .WithSummary("Get releases summary")
            .WithDescription(Description)
            .Produces<ReleasesSummaryResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        app.MapGet("releases/buckets", ReleasesBuckets)
            .WithName("ReleasesBuckets")
            .WithTags(GroupName)
            .WithSummary("Get releases buckets by day or hour")
            .WithDescription(Description)
            .Produces<BucketsResponse<BucketResponse<ReleasesSummaryResponse>>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();
    }

    /// <param name="from" example="2025-09-10T11:08:48Z">ISO 8609 UTC only</param>
    /// <param name="to" example="2025-09-11T11:08:48Z">ISO 8609 UTC only</param>
    /// <param name="reportRepository"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet]
    private static async Task<IResult> ReleasesSummary(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromServices] IReportRepository reportRepository,
        CancellationToken cancellationToken
    )
    {
        var errors = ValidateRequest(from, to);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var releasesSummary = await reportRepository.GetReleasesSummary(from, to, cancellationToken);

        return Results.Ok(releasesSummary.ToResponse());
    }

    /// <param name="from" example="2025-09-10T11:08:48Z">ISO 8609 UTC only</param>
    /// <param name="to" example="2025-09-11T11:08:48Z">ISO 8609 UTC only</param>
    /// <param name="unit">"hour" or "day"</param>
    /// <param name="reportRepository"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet]
    private static async Task<IResult> ReleasesBuckets(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] string unit,
        [FromServices] IReportRepository reportRepository,
        CancellationToken cancellationToken
    )
    {
        var errors = ValidateRequest(from, to, unit);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var releasesBuckets = await reportRepository.GetReleasesBuckets(from, to, unit, cancellationToken);

        return Results.Ok(releasesBuckets.ToResponse());
    }

    /// <param name="from" example="2025-09-10T11:08:48Z">ISO 8609 UTC only</param>
    /// <param name="to" example="2025-09-11T11:08:48Z">ISO 8609 UTC only</param>
    /// <param name="reportRepository"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet]
    private static async Task<IResult> MatchesSummary(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromServices] IReportRepository reportRepository,
        CancellationToken cancellationToken
    )
    {
        var errors = ValidateRequest(from, to);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var matchesSummary = await reportRepository.GetMatchesSummary(from, to, cancellationToken);

        return Results.Ok(matchesSummary.ToResponse());
    }

    /// <param name="from" example="2025-09-10T11:08:48Z">ISO 8609 UTC only</param>
    /// <param name="to" example="2025-09-11T11:08:48Z">ISO 8609 UTC only</param>
    /// <param name="unit">"hour" or "day"</param>
    /// <param name="reportRepository"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet]
    private static async Task<IResult> MatchesBuckets(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] string unit,
        [FromServices] IReportRepository reportRepository,
        CancellationToken cancellationToken
    )
    {
        var errors = ValidateRequest(from, to, unit);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var matchesBuckets = await reportRepository.GetMatchesBuckets(from, to, unit, cancellationToken);

        return Results.Ok(matchesBuckets.ToResponse());
    }

    /// <param name="from" example="2025-09-10T11:08:48Z">ISO 8609 UTC only</param>
    /// <param name="to" example="2025-09-11T11:08:48Z">ISO 8609 UTC only</param>
    /// <param name="reportRepository"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet]
    private static async Task<IResult> ClearanceRequestsSummary(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromServices] IReportRepository reportRepository,
        CancellationToken cancellationToken
    )
    {
        var errors = ValidateRequest(from, to);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var clearanceRequestsSummary = await reportRepository.GetClearanceRequestsSummary(from, to, cancellationToken);

        return Results.Ok(clearanceRequestsSummary.ToResponse());
    }

    /// <param name="from" example="2025-09-10T11:08:48Z">ISO 8609 UTC only</param>
    /// <param name="to" example="2025-09-11T11:08:48Z">ISO 8609 UTC only</param>
    /// <param name="unit">"hour" or "day"</param>
    /// <param name="reportRepository"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet]
    private static async Task<IResult> ClearanceRequestsBuckets(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] string unit,
        [FromServices] IReportRepository reportRepository,
        CancellationToken cancellationToken
    )
    {
        var errors = ValidateRequest(from, to, unit);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var clearanceRequestsBuckets = await reportRepository.GetClearanceRequestsBuckets(
            from,
            to,
            unit,
            cancellationToken
        );

        return Results.Ok(clearanceRequestsBuckets.ToResponse());
    }

    /// <param name="from" example="2025-09-10T11:08:48Z">ISO 8609 UTC only</param>
    /// <param name="to" example="2025-09-11T11:08:48Z">ISO 8609 UTC only</param>
    /// <param name="reportRepository"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet]
    private static async Task<IResult> NotificationsSummary(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromServices] IReportRepository reportRepository,
        CancellationToken cancellationToken
    )
    {
        var errors = ValidateRequest(from, to);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var notificationsSummary = await reportRepository.GetNotificationsSummary(from, to, cancellationToken);

        return Results.Ok(notificationsSummary.ToResponse());
    }

    /// <param name="from" example="2025-09-10T11:08:48Z">ISO 8609 UTC only</param>
    /// <param name="to" example="2025-09-11T11:08:48Z">ISO 8609 UTC only</param>
    /// <param name="unit">"hour" or "day"</param>
    /// <param name="reportRepository"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet]
    private static async Task<IResult> NotificationsBuckets(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] string unit,
        [FromServices] IReportRepository reportRepository,
        CancellationToken cancellationToken
    )
    {
        var errors = ValidateRequest(from, to, unit);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var notificationsBuckets = await reportRepository.GetNotificationsBuckets(from, to, unit, cancellationToken);

        return Results.Ok(notificationsBuckets.ToResponse());
    }

    /// <param name="from" example="2025-09-10T11:08:48Z">ISO 8609 UTC only</param>
    /// <param name="to" example="2025-09-11T11:08:48Z">ISO 8609 UTC only</param>
    /// <param name="reportRepository"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet]
    private static async Task<IResult> Summary(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromServices] IReportRepository reportRepository,
        CancellationToken cancellationToken
    )
    {
        var errors = ValidateRequest(from, to);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var releasesTask = reportRepository.GetReleasesSummary(from, to, cancellationToken);
        var matchesTask = reportRepository.GetMatchesSummary(from, to, cancellationToken);
        var clearanceRequestsTask = reportRepository.GetClearanceRequestsSummary(from, to, cancellationToken);
        var notificationsTask = reportRepository.GetNotificationsSummary(from, to, cancellationToken);

        await Task.WhenAll(releasesTask, matchesTask, clearanceRequestsTask, notificationsTask);

        var releases = await releasesTask;
        var matches = await matchesTask;
        var clearanceRequests = await clearanceRequestsTask;
        var notifications = await notificationsTask;

        return Results.Ok(
            new SummaryResponse(
                releases.ToResponse(),
                matches.ToResponse(),
                clearanceRequests.ToResponse(),
                notifications.ToResponse()
            )
        );
    }

    /// <param name="from" example="2025-09-10T11:08:48Z">ISO 8609 UTC only</param>
    /// <param name="to" example="2025-09-11T11:08:48Z">ISO 8609 UTC only</param>
    /// <param name="unit">"hour" or "day"</param>
    /// <param name="reportRepository"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet]
    private static async Task<IResult> Buckets(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] string unit,
        [FromServices] IReportRepository reportRepository,
        CancellationToken cancellationToken
    )
    {
        var errors = ValidateRequest(from, to, unit);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var releasesTask = reportRepository.GetReleasesBuckets(from, to, unit, cancellationToken);
        var matchesTask = reportRepository.GetMatchesBuckets(from, to, unit, cancellationToken);
        var clearanceRequestsTask = reportRepository.GetClearanceRequestsBuckets(from, to, unit, cancellationToken);
        var notificationsTask = reportRepository.GetNotificationsBuckets(from, to, unit, cancellationToken);

        await Task.WhenAll(releasesTask, matchesTask, clearanceRequestsTask, notificationsTask);

        var releases = await releasesTask;
        var matches = await matchesTask;
        var clearanceRequests = await clearanceRequestsTask;
        var notifications = await notificationsTask;

        return Results.Ok(
            new BucketsResponse(
                releases.ToResponse(),
                matches.ToResponse(),
                clearanceRequests.ToResponse(),
                notifications.ToResponse()
            )
        );
    }

    [HttpGet]
    private static async Task<IResult> LastReceived(
        [FromServices] IReportRepository reportRepository,
        CancellationToken cancellationToken
    )
    {
        var lastReceived = await reportRepository.GetLastReceivedSummary(cancellationToken);

        return Results.Ok(
            new LastReceivedResponse(
                lastReceived.Finalisation is not null
                    ? new LastReceivedMessageResponse(
                        lastReceived.Finalisation.Timestamp,
                        lastReceived.Finalisation.Reference
                    )
                    : null,
                lastReceived.Request is not null
                    ? new LastReceivedMessageResponse(lastReceived.Request.Timestamp, lastReceived.Request.Reference)
                    : null
            )
        );
    }

    private static Dictionary<string, string[]> ValidateRequest(DateTime from, DateTime to, string? unit = null)
    {
        var errors = new Dictionary<string, string[]>();

        if (from > to)
        {
            errors.Add("from", ["from cannot be greater than to"]);
        }

        if (to.Subtract(from).Days > 31)
        {
            errors.Add("", ["date span cannot be greater than 31 days"]);
        }

        if (from.Kind != DateTimeKind.Utc)
        {
            errors.Add("from", ["date must be UTC"]);
        }

        if (to.Kind != DateTimeKind.Utc)
        {
            errors.Add("to", ["date must be UTC"]);
        }

        if (unit is not null && unit != "hour" && unit != "day")
        {
            errors.Add("unit", ["unit must be 'hour' or 'day'"]);
        }

        return errors;
    }
}
