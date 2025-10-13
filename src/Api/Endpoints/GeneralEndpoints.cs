using Defra.TradeImportsReportingApi.Api.Data;
using Defra.TradeImportsReportingApi.Api.Endpoints.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Defra.TradeImportsReportingApi.Api.Endpoints;

public static class GeneralEndpoints
{
    public static void MapGeneralEndpoints(this IEndpointRouteBuilder app)
    {
        const string general = "General";

        app.MapGet("summary", Summary)
            .WithName(nameof(Summary))
            .WithTags(general)
            .WithSummary("Get summary")
            .WithDescription(Descriptions.SearchablePeriod)
            .Produces<SummaryResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        app.MapGet("buckets", Buckets)
            .WithName(nameof(Buckets))
            .WithTags(general)
            .WithSummary("Get buckets by day or hour")
            .WithDescription(Descriptions.SearchablePeriod)
            .Produces<IntervalsResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        app.MapGet("intervals", Intervals)
            .WithName(nameof(Intervals))
            .WithTags(general)
            .WithSummary("Get by interval")
            .WithDescription(Descriptions.SearchablePeriod)
            .Produces<IntervalsResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        const string statusInformation = "Status Information";

        app.MapGet("last-received", LastReceived)
            .WithName(nameof(LastReceived))
            .WithTags(statusInformation)
            .WithSummary("Get last received")
            .Produces<LastReceivedResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        app.MapGet("last-sent", LastSent)
            .WithName(nameof(LastSent))
            .WithTags(statusInformation)
            .WithSummary("Get last sent")
            .Produces<LastSentResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        app.MapGet("status", Status)
            .WithName(nameof(Status))
            .WithTags(statusInformation)
            .WithSummary("Get status")
            .Produces<StatusResponse>()
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
    private static async Task<IResult> Summary(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromServices] IReportRepository reportRepository,
        CancellationToken cancellationToken
    )
    {
        var errors = Request.Validate(from, to);
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
        var errors = Request.Validate(from, to, unit);
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
            new IntervalsResponse(
                releases.ToResponse(),
                matches.ToResponse(),
                clearanceRequests.ToResponse(),
                notifications.ToResponse()
            )
        );
    }

    /// <param name="from" example="2025-09-10T11:08:48Z">ISO 8609 UTC only</param>
    /// <param name="to" example="2025-09-11T11:08:48Z">ISO 8609 UTC only</param>
    /// <param name="intervals">ISO 8609 UTC only, sequential list of values. Note values should be specified as ?intervals=X&amp;intervals=X</param>
    /// <param name="reportRepository"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet]
    private static async Task<IResult> Intervals(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] DateTime[] intervals,
        [FromServices] IReportRepository reportRepository,
        CancellationToken cancellationToken
    )
    {
        var errors = Request.Validate(from, to, intervals: intervals);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var releasesTask = reportRepository.GetReleasesIntervals(from, to, intervals, cancellationToken);
        var matchesTask = reportRepository.GetMatchesIntervals(from, to, intervals, cancellationToken);
        var clearanceRequestsTask = reportRepository.GetClearanceRequestsIntervals(
            from,
            to,
            intervals,
            cancellationToken
        );
        var notificationsTask = reportRepository.GetNotificationsIntervals(from, to, intervals, cancellationToken);

        await Task.WhenAll(releasesTask, matchesTask, clearanceRequestsTask, notificationsTask);

        var releases = await releasesTask;
        var matches = await matchesTask;
        var clearanceRequests = await clearanceRequestsTask;
        var notifications = await notificationsTask;

        return Results.Ok(
            new IntervalsResponse(
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

        return Results.Ok(lastReceived.ToResponse());
    }

    [HttpGet]
    private static async Task<IResult> LastSent(
        [FromServices] IReportRepository reportRepository,
        CancellationToken cancellationToken
    )
    {
        var lastSent = await reportRepository.GetLastSentSummary(cancellationToken);

        return Results.Ok(lastSent.ToResponse());
    }

    [HttpGet]
    private static async Task<IResult> Status(
        [FromServices] IReportRepository reportRepository,
        CancellationToken cancellationToken
    )
    {
        var lastReceivedTask = reportRepository.GetLastReceivedSummary(cancellationToken);
        var lastSentTask = reportRepository.GetLastSentSummary(cancellationToken);

        await Task.WhenAll(lastReceivedTask, lastSentTask);

        var lastReceived = await lastReceivedTask;
        var lastSent = await lastSentTask;

        return Results.Ok(new StatusResponse(lastReceived.ToResponse(), lastSent.ToResponse()));
    }
}
