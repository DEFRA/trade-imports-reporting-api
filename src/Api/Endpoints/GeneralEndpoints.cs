using Defra.TradeImportsReportingApi.Api.Data;
using Defra.TradeImportsReportingApi.Api.Endpoints.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Defra.TradeImportsReportingApi.Api.Endpoints;

public static class GeneralEndpoints
{
    public static void MapGeneralEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("summary", Summary)
            .WithName("Summary")
            .WithTags("General")
            .WithSummary("Get summary")
            .WithDescription(Descriptions.SearchablePeriod)
            .Produces<SummaryResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        app.MapGet("buckets", Buckets)
            .WithName("Buckets")
            .WithTags("General")
            .WithSummary("Get buckets by day or hour")
            .WithDescription(Descriptions.SearchablePeriod)
            .Produces<IntervalsResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        app.MapGet("intervals", Intervals)
            .WithName("Intervals")
            .WithTags("General")
            .WithSummary("Get by interval")
            .WithDescription(Descriptions.SearchablePeriod)
            .Produces<IntervalsResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        app.MapGet("last-received", LastReceived)
            .WithName("LastReceived")
            .WithTags("Status Information")
            .WithSummary("Get last received")
            .Produces<LastReceivedResponse>()
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
                    : null,
                lastReceived.Notification is not null
                    ? new LastReceivedMessageResponse(
                        lastReceived.Notification.Timestamp,
                        lastReceived.Notification.Reference
                    )
                    : null
            )
        );
    }
}
