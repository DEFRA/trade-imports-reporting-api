using Defra.TradeImportsReportingApi.Api.Data;
using Defra.TradeImportsReportingApi.Api.Endpoints.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Defra.TradeImportsReportingApi.Api.Endpoints;

public static class ReleasesEndpoints
{
    public static void MapReleasesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("releases/summary", ReleasesSummary)
            .WithName("ReleasesSummary")
            .WithTags("Finalisations")
            .WithSummary("Get releases summary")
            .WithDescription(Descriptions.SearchablePeriod)
            .Produces<ReleasesSummaryResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        app.MapGet("releases/intervals", ReleasesIntervals)
            .WithName("ReleasesIntervals")
            .WithTags("Finalisations")
            .WithSummary("Get releases by interval")
            .WithDescription(Descriptions.SearchablePeriod)
            .Produces<IntervalsResponse<IntervalResponse<ReleasesSummaryResponse>>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        app.MapGet("releases/data", ReleasesData)
            .WithName("ReleasesData")
            .WithTags("Finalisations")
            .WithSummary("Get releases data")
            .WithDescription(Descriptions.SearchablePeriod)
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
        var errors = Request.Validate(from, to);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var releasesSummary = await reportRepository.GetReleasesSummary(from, to, cancellationToken);

        return Results.Ok(releasesSummary.ToResponse());
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
        var errors = Request.Validate(from, to, intervals: intervals);
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
        var errors = Request.Validate(from, to, releaseType: releaseType);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var releasesData = await reportRepository.GetReleases(from, to, releaseType, cancellationToken);

        return Request.IsCsvRequired(httpContext)
            ? Request.CsvResult(releasesData.ToCsvResponse())
            : Results.Ok(releasesData.ToResponse());
    }
}
