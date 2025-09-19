using System.Text;

namespace Defra.TradeImportsReportingApi.Api.Endpoints;

public static class CsvRequest
{
    private const string CsvContentType = "text/csv";

    public static IResult Result(string content) => Results.Text(content, CsvContentType, Encoding.UTF8);

    public static bool IsCsvRequired(HttpContext httpContext) =>
        httpContext.Request.Headers.Accept.ToString().Contains(CsvContentType, StringComparison.OrdinalIgnoreCase);
}
