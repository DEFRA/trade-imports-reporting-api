using Defra.TradeImportsReportingApi.Api.Data;
using Microsoft.AspNetCore.Mvc;

namespace Defra.TradeImportsReportingApi.Api.Endpoints;

public static class EndpointRouteBuilderExtensions
{
    public static void MapEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("releases/summary", ReleasesSummary).RequireAuthorization();
    }

    [HttpGet]
    private static async Task<IResult> ReleasesSummary(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromServices] IReportRepository reportRepository,
        CancellationToken cancellationToken
    )
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

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var releasesSummary = await reportRepository.GetReleasesSummary(from, to, cancellationToken);

        return Results.Ok(
            new ReleasesSummaryResponse(releasesSummary.Automatic, releasesSummary.Manual, releasesSummary.Total)
        );
    }
}
