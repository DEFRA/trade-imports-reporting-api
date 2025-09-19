using System.Text;
using Defra.TradeImportsReportingApi.Api.Data;
using Defra.TradeImportsReportingApi.Api.Data.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Defra.TradeImportsReportingApi.Api.Endpoints;

public static class EndpointRouteBuilderExtensions
{
    private static readonly string s_description = $"Searchable period is the last {TimePeriod.MaxDays} days";

    public static void MapEndpoints(this IEndpointRouteBuilder app)
    {
        MapReleasesEndpoints(app);
        MapMatchesEndpoints(app);
        MapClearanceRequestEndpoints(app);
        MapNotificationEndpoints(app);
        MapGeneralEndpoints(app);

        app.MapGet("last-received", LastReceived)
            .WithName("LastReceived")
            .WithTags("Status Information")
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
            .WithTags("General")
            .WithSummary("Get summary")
            .WithDescription(s_description)
            .Produces<SummaryResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        app.MapGet("buckets", Buckets)
            .WithName("Buckets")
            .WithTags("General")
            .WithSummary("Get buckets by day or hour")
            .WithDescription(s_description)
            .Produces<BucketsResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();
    }

    private static void MapNotificationEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("notifications/summary", NotificationsSummary)
            .WithName("NotificationsSummary")
            .WithTags("Notifications")
            .WithSummary("Get notifications summary")
            .WithDescription(s_description)
            .Produces<NotificationsSummaryResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        app.MapGet("notifications/buckets", NotificationsBuckets)
            .WithName("NotificationsBuckets")
            .WithTags("Notifications")
            .WithSummary("Get notifications buckets by day or hour")
            .WithDescription(s_description)
            .Produces<BucketsResponse<BucketResponse<NotificationsSummaryResponse>>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        app.MapGet("notifications/intervals", NotificationsIntervals)
            .WithName("NotificationsIntervals")
            .WithTags("Notifications")
            .WithSummary("Get notifications by interval")
            .WithDescription(s_description)
            .Produces<BucketsResponse<BucketResponse<NotificationsSummaryResponse>>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();
    }

    private static void MapClearanceRequestEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("clearance-requests/summary", ClearanceRequestsSummary)
            .WithName("ClearanceRequestsSummary")
            .WithTags("Clearance Requests")
            .WithSummary("Get clearance requests summary")
            .WithDescription(s_description)
            .Produces<ClearanceRequestsSummaryResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        app.MapGet("clearance-requests/buckets", ClearanceRequestsBuckets)
            .WithName("ClearanceRequestsBuckets")
            .WithTags("Clearance Requests")
            .WithSummary("Get clearance requests buckets by day or hour")
            .WithDescription(s_description)
            .Produces<BucketsResponse<BucketResponse<ClearanceRequestsSummaryBucketResponse>>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        app.MapGet("clearance-requests/intervals", ClearanceRequestsIntervals)
            .WithName("ClearanceRequestsIntervals")
            .WithTags("Clearance Requests")
            .WithSummary("Get clearance requests by interval")
            .WithDescription(s_description)
            .Produces<BucketsResponse<BucketResponse<ClearanceRequestsSummaryBucketResponse>>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();
    }

    private static void MapMatchesEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("matches/summary", MatchesSummary)
            .WithName("MatchesSummary")
            .WithTags("Decisions")
            .WithSummary("Get matches summary")
            .WithDescription(s_description)
            .Produces<MatchesSummaryResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        app.MapGet("matches/buckets", MatchesBuckets)
            .WithName("MatchesBuckets")
            .WithTags("Decisions")
            .WithSummary("Get matches buckets by day or hour")
            .WithDescription(s_description)
            .Produces<BucketsResponse<BucketResponse<MatchesSummaryResponse>>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        app.MapGet("matches/intervals", MatchesIntervals)
            .WithName("MatchesIntervals")
            .WithTags("Decisions")
            .WithSummary("Get matches by interval")
            .WithDescription(s_description)
            .Produces<BucketsResponse<BucketResponse<MatchesSummaryResponse>>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        app.MapGet("matches/data", MatchesData)
            .WithName("MatchesData")
            .WithTags("Decisions")
            .WithSummary("Get matches data")
            .WithDescription(s_description)
            .Produces<DatumResponse<MatchResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();
    }

    private static void MapReleasesEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("releases/summary", ReleasesSummary)
            .WithName("ReleasesSummary")
            .WithTags("Finalisations")
            .WithSummary("Get releases summary")
            .WithDescription(s_description)
            .Produces<ReleasesSummaryResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        app.MapGet("releases/buckets", ReleasesBuckets)
            .WithName("ReleasesBuckets")
            .WithTags("Finalisations")
            .WithSummary("Get releases buckets by day or hour")
            .WithDescription(s_description)
            .Produces<BucketsResponse<BucketResponse<ReleasesSummaryResponse>>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        app.MapGet("releases/intervals", ReleasesIntervals)
            .WithName("ReleasesIntervals")
            .WithTags("Finalisations")
            .WithSummary("Get releases by interval")
            .WithDescription(s_description)
            .Produces<BucketsResponse<BucketResponse<ReleasesSummaryResponse>>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        app.MapGet("releases/data", ReleasesData)
            .WithName("ReleasesData")
            .WithTags("Finalisations")
            .WithSummary("Get releases data")
            .WithDescription(s_description)
            .Produces<DatumResponse<ReleasesResponse>>()
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
    /// <param name="intervals">ISO 8609 UTC only, sequential list of values. Note values should be specified as ?intervals=X&amp;intervals=X</param>
    /// <param name="reportRepository"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet]
    private static async Task<IResult> ReleasesIntervals(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] DateTime[] intervals,
        [FromServices] IReportRepository reportRepository,
        CancellationToken cancellationToken
    )
    {
        var errors = ValidateRequest(from, to, intervals: intervals);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var releasesIntervals = await reportRepository.GetReleasesIntervals(from, to, intervals, cancellationToken);

        return Results.Ok(releasesIntervals.ToResponse());
    }

    /// <param name="from" example="2025-09-10T11:08:48Z">ISO 8609 UTC only</param>
    /// <param name="to" example="2025-09-11T11:08:48Z">ISO 8609 UTC only</param>
    /// <param name="releaseType">"Automatic" or "Manual" or "Cancelled"</param>
    /// <param name="reportRepository"></param>
    /// <param name="httpContext"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet]
    private static async Task<IResult> ReleasesData(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] string releaseType,
        [FromServices] IReportRepository reportRepository,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var errors = ValidateRequest(from, to, releaseType: releaseType);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var releasesData = await reportRepository.GetReleases(from, to, releaseType, cancellationToken);

        return RequireCsv(httpContext)
            ? CsvResult(releasesData.ToCsvResponse())
            : Results.Ok(releasesData.ToResponse());
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
    /// <param name="intervals">ISO 8609 UTC only, sequential list of values. Note values should be specified as ?intervals=X&amp;intervals=X</param>
    /// <param name="reportRepository"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet]
    private static async Task<IResult> MatchesIntervals(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] DateTime[] intervals,
        [FromServices] IReportRepository reportRepository,
        CancellationToken cancellationToken
    )
    {
        var errors = ValidateRequest(from, to, intervals: intervals);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var matchesIntervals = await reportRepository.GetMatchesIntervals(from, to, intervals, cancellationToken);

        return Results.Ok(matchesIntervals.ToResponse());
    }

    /// <param name="from" example="2025-09-10T11:08:48Z">ISO 8609 UTC only</param>
    /// <param name="to" example="2025-09-11T11:08:48Z">ISO 8609 UTC only</param>
    /// <param name="match">true or false</param>
    /// <param name="reportRepository"></param>
    /// <param name="httpContext"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet]
    private static async Task<IResult> MatchesData(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] bool match,
        [FromServices] IReportRepository reportRepository,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var errors = ValidateRequest(from, to);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var matchesData = await reportRepository.GetMatches(from, to, match, cancellationToken);

        return RequireCsv(httpContext) ? CsvResult(matchesData.ToCsvResponse()) : Results.Ok(matchesData.ToResponse());
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
    /// <param name="intervals">ISO 8609 UTC only, sequential list of values. Note values should be specified as ?intervals=X&amp;intervals=X</param>
    /// <param name="reportRepository"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet]
    private static async Task<IResult> ClearanceRequestsIntervals(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] DateTime[] intervals,
        [FromServices] IReportRepository reportRepository,
        CancellationToken cancellationToken
    )
    {
        var errors = ValidateRequest(from, to, intervals: intervals);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var clearanceRequestsIntervals = await reportRepository.GetClearanceRequestsIntervals(
            from,
            to,
            intervals,
            cancellationToken
        );

        return Results.Ok(clearanceRequestsIntervals.ToResponse());
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
    /// <param name="intervals">ISO 8609 UTC only, sequential list of values. Note values should be specified as ?intervals=X&amp;intervals=X</param>
    /// <param name="reportRepository"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet]
    private static async Task<IResult> NotificationsIntervals(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] DateTime[] intervals,
        [FromServices] IReportRepository reportRepository,
        CancellationToken cancellationToken
    )
    {
        var errors = ValidateRequest(from, to, intervals: intervals);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var notificationsIntervals = await reportRepository.GetNotificationsIntervals(
            from,
            to,
            intervals,
            cancellationToken
        );

        return Results.Ok(notificationsIntervals.ToResponse());
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

    private static Dictionary<string, string[]> ValidateRequest(
        DateTime from,
        DateTime to,
        string? unit = null,
        string? releaseType = null,
        DateTime[]? intervals = null
    )
    {
        var errors = new Dictionary<string, string[]>();

        if (from > to)
        {
            errors.Add("from", ["from cannot be greater than to"]);
        }

        if (to.Subtract(from).Days > TimePeriod.MaxDays)
        {
            errors.Add("", [$"date span cannot be greater than {TimePeriod.MaxDays} days"]);
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

        if (
            releaseType is not null
            && releaseType != ReleaseType.Automatic
            && releaseType != ReleaseType.Manual
            && releaseType != ReleaseType.Cancelled
        )
        {
            errors.Add(
                "releaseType",
                [
                    $"release type must be '{ReleaseType.Automatic}' or '{ReleaseType.Manual}' or '{ReleaseType.Cancelled}'",
                ]
            );
        }

        if (intervals is not null && intervals.Any(interval => interval.Kind != DateTimeKind.Utc))
        {
            errors.Add("intervals", ["date(s) must be UTC"]);
        }

        return errors;
    }

    public static class TimePeriod
    {
        public const int MaxDays = 122; // Roughly 4 months based on 365/12 x 4
    }

    private const string CsvContentType = "text/csv";

    private static IResult CsvResult(string content) => Results.Text(content, CsvContentType, Encoding.UTF8);

    private static bool RequireCsv(HttpContext httpContext) =>
        httpContext.Request.Headers.Accept.ToString().Contains(CsvContentType, StringComparison.OrdinalIgnoreCase);
}
