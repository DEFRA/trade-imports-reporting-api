using System.Text;
using Defra.TradeImportsReportingApi.Api.Data;
using Defra.TradeImportsReportingApi.Api.Data.Entities;

namespace Defra.TradeImportsReportingApi.Api.Endpoints;

public static class Request
{
    public static Dictionary<string, string[]> Validate(
        DateTime from,
        DateTime to,
        string? unit = null,
        string? releaseType = null,
        DateTime[]? intervals = null,
        bool? match = null,
        int? matchLevel = null
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

        if (matchLevel is not null)
        {
            if (match is false)
            {
                errors.Add("matchLevel", ["matchLevel must not be set if match is false"]);
            }

            if (matchLevel is not (1 or 2 or 3))
            {
                errors.Add("matchLevel", ["matchLevel must be 1 or 2 or 3 if specified"]);
            }
        }

        return errors;
    }

    private const string CsvContentType = "text/csv";

    public static IResult CsvResult(string content) => Results.Text(content, CsvContentType, Encoding.UTF8);

    public static bool IsCsvRequired(HttpContext httpContext) =>
        httpContext.Request.Headers.Accept.ToString().Contains(CsvContentType, StringComparison.OrdinalIgnoreCase);
}
