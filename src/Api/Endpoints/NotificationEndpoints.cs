using Defra.TradeImportsReportingApi.Api.Data;
using Defra.TradeImportsReportingApi.Api.Endpoints.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Defra.TradeImportsReportingApi.Api.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("notifications/summary", NotificationsSummary)
            .WithName("NotificationsSummary")
            .WithTags("Notifications")
            .WithSummary("Get notifications summary")
            .WithDescription(Descriptions.SearchablePeriod)
            .Produces<NotificationsSummaryResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        app.MapGet("notifications/intervals", NotificationsIntervals)
            .WithName("NotificationsIntervals")
            .WithTags("Notifications")
            .WithSummary("Get notifications by interval")
            .WithDescription(Descriptions.SearchablePeriod)
            .Produces<IntervalsResponse<IntervalResponse<NotificationsSummaryResponse>>>()
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
    private static async Task<IResult> NotificationsSummary(
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

        var notificationsSummary = await reportRepository.GetNotificationsSummary(from, to, cancellationToken);

        return Results.Ok(notificationsSummary.ToResponse());
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
        var errors = Request.Validate(from, to, intervals: intervals);
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
}
