using Defra.TradeImportsReportingApi.Api.Data;
using Defra.TradeImportsReportingApi.Api.Endpoints.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Defra.TradeImportsReportingApi.Api.Endpoints;

public static class ClearanceRequestEndpoints
{
    public static void MapClearanceRequestEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("clearance-requests/summary", ClearanceRequestsSummary)
            .WithName("ClearanceRequestsSummary")
            .WithTags("Clearance Requests")
            .WithSummary("Get clearance requests summary")
            .WithDescription(Descriptions.SearchablePeriod)
            .Produces<ClearanceRequestsSummaryResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        app.MapGet("clearance-requests/buckets", ClearanceRequestsBuckets)
            .WithName("ClearanceRequestsBuckets")
            .WithTags("Clearance Requests")
            .WithSummary("Get clearance requests buckets by day or hour")
            .WithDescription(Descriptions.SearchablePeriod)
            .Produces<IntervalsResponse<IntervalResponse<ClearanceRequestsSummaryResponse>>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        app.MapGet("clearance-requests/intervals", ClearanceRequestsIntervals)
            .WithName("ClearanceRequestsIntervals")
            .WithTags("Clearance Requests")
            .WithSummary("Get clearance requests by interval")
            .WithDescription(Descriptions.SearchablePeriod)
            .Produces<IntervalsResponse<IntervalResponse<ClearanceRequestsSummaryResponse>>>()
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
    private static async Task<IResult> ClearanceRequestsSummary(
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
        var errors = Request.Validate(from, to, unit);
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
        var errors = Request.Validate(from, to, intervals: intervals);
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
}
