using Defra.TradeImportsReportingApi.Api.Data;
using Microsoft.AspNetCore.Mvc;

namespace Defra.TradeImportsReportingApi.Api.Endpoints;

public static class EndpointRouteBuilderExtensions
{
    public static void MapEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("releases/summary", ReleasesSummary).RequireAuthorization();
        app.MapGet("matches/summary", MatchesSummary).RequireAuthorization();
        app.MapGet("clearance-requests/summary", ClearanceRequestsSummary).RequireAuthorization();
        app.MapGet("notifications/summary", NotificationsSummary).RequireAuthorization();

        app.MapGet("summary", Summary).RequireAuthorization();

        app.MapGet("last-received", LastReceived).RequireAuthorization();
    }

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

        var releasesSummaryTask = reportRepository.GetReleasesSummary(from, to, cancellationToken);
        var matchesSummaryTask = reportRepository.GetMatchesSummary(from, to, cancellationToken);
        var clearanceRequestsSummaryTask = reportRepository.GetClearanceRequestsSummary(from, to, cancellationToken);
        var notificationsTask = reportRepository.GetNotificationsSummary(from, to, cancellationToken);

        await Task.WhenAll(releasesSummaryTask, matchesSummaryTask, clearanceRequestsSummaryTask, notificationsTask);

        var releasesSummary = await releasesSummaryTask;
        var matchesSummary = await matchesSummaryTask;
        var clearanceRequestsSummary = await clearanceRequestsSummaryTask;
        var notificationsSummary = await notificationsTask;

        return Results.Ok(
            new SummaryResponse(
                releasesSummary.ToResponse(),
                matchesSummary.ToResponse(),
                clearanceRequestsSummary.ToResponse(),
                notificationsSummary.ToResponse()
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

    private static Dictionary<string, string[]> ValidateRequest(DateTime from, DateTime to)
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

        return errors;
    }
}
