using Defra.TradeImportsReportingApi.Api.Data;
using Defra.TradeImportsReportingApi.Api.Endpoints.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Defra.TradeImportsReportingApi.Api.Endpoints;

public static class MatchesEndpoints
{
    public static void MapMatchesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("matches/summary", MatchesSummary)
            .WithName("MatchesSummary")
            .WithTags("Decisions")
            .WithSummary("Get matches summary")
            .WithDescription(Descriptions.SearchablePeriod)
            .Produces<MatchesSummaryResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        app.MapGet("matches/intervals", MatchesIntervals)
            .WithName("MatchesIntervals")
            .WithTags("Decisions")
            .WithSummary("Get matches by interval")
            .WithDescription(Descriptions.SearchablePeriod)
            .Produces<IntervalsResponse<IntervalResponse<MatchesSummaryResponse>>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        app.MapGet("matches/data", MatchesData)
            .WithName("MatchesData")
            .WithTags("Decisions")
            .WithSummary("Get matches data")
            .WithDescription(Descriptions.SearchablePeriod)
            .Produces<DatumResponse<MatchResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        app.MapGet("matches/summary/levels", MatchesSummaryByLevel)
            .WithName("MatchesSummaryByLevel")
            .WithTags("Decisions")
            .WithSummary("Get matches summary counts by level")
            .WithDescription(Descriptions.SearchablePeriod)
            .Produces<MatchesSummaryByLevelResponse>()
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
    private static async Task<IResult> MatchesSummary(
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

        var matchesSummary = await reportRepository.GetMatchesSummary(from, to, cancellationToken);

        return Results.Ok(matchesSummary.ToResponse());
    }

    /// <param name="from" example="2025-09-10T11:08:48Z">ISO 8609 UTC only</param>
    /// <param name="to" example="2025-09-11T11:08:48Z">ISO 8609 UTC only</param>
    /// <param name="reportRepository"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet]
    private static async Task<IResult> MatchesSummaryByLevel(
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

        var matchesSummaryByLevel = await reportRepository.GetMatchesSummaryByLevel(from, to, cancellationToken);

        return Results.Ok(matchesSummaryByLevel.ToResponse());
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
        var errors = Request.Validate(from, to, intervals: intervals);
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
    /// <param name="useV2"></param>
    /// <param name="reportRepository"></param>
    /// <param name="httpContext"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet]
    private static async Task<IResult> MatchesData(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] bool match,
        [FromHeader] bool? useV2,
        [FromServices] IReportRepository reportRepository,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var errors = Request.Validate(from, to);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        if (useV2.GetValueOrDefault())
        {
            var v2Data = await reportRepository.GetMatchesV2(from, to, match, cancellationToken);

            return Request.IsCsvRequired(httpContext)
                ? Request.CsvResult(v2Data.ToCsvResponse())
                : Results.Ok(v2Data.ToResponse());
        }

        var v1Data = await reportRepository.GetMatches(from, to, match, cancellationToken);

        return Request.IsCsvRequired(httpContext)
            ? Request.CsvResult(v1Data.ToCsvResponse())
            : Results.Ok(v1Data.ToResponse());
    }
}
