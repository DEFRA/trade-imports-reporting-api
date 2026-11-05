using Defra.TradeImportsReportingApi.Api.Data;
using Defra.TradeImportsReportingApi.Api.Endpoints.Dtos;
using Microsoft.AspNetCore.Mvc;
using static Defra.TradeImportsReportingApi.Api.Endpoints.Dtos.ChedExtractResponse;
using static Defra.TradeImportsReportingApi.Api.Endpoints.Dtos.CustomsDeclarationExtractResponse;

namespace Defra.TradeImportsReportingApi.Api.Endpoints;

public static class ExtractEndpoints
{
    private const string ExtractsTag = "Extracts";

    private static readonly string s_description =
        $"{Descriptions.SearchablePeriod}. Returns one row per commodity for records last updated within the "
        + "date range. Set the Accept header to text/csv for a CSV response.";

    public static void MapExtractEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("extracts/ipaffs-cheds", IpaffsCheds)
            .WithName("IpaffsChedsExtract")
            .WithTags(ExtractsTag)
            .WithSummary("Get IPAFFS CHEDs extract")
            .WithDescription(s_description)
            .Produces<DatumResponse<ChedExtractResponse>>()
            .Produces<string>(StatusCodes.Status200OK, "text/csv")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        app.MapGet("extracts/traces-cheds", TracesCheds)
            .WithName("TracesChedsExtract")
            .WithTags(ExtractsTag)
            .WithSummary("Get TRACES CHEDs extract")
            .WithDescription(s_description)
            .Produces<DatumResponse<ChedExtractResponse>>()
            .Produces<string>(StatusCodes.Status200OK, "text/csv")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

        app.MapGet("extracts/customs-declarations", CustomsDeclarations)
            .WithName("CustomsDeclarationsExtract")
            .WithTags(ExtractsTag)
            .WithSummary("Get customs declarations extract")
            .WithDescription(s_description)
            .Produces<DatumResponse<CustomsDeclarationExtractResponse>>()
            .Produces<string>(StatusCodes.Status200OK, "text/csv")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();
    }

    /// <param name="request">The <see cref="DateRangeRequest"/> object</param>
    /// <param name="extractRepository"></param>
    /// <param name="httpContext"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet]
    private static async Task<IResult> IpaffsCheds(
        [AsParameters] DateRangeRequest request,
        [FromServices] IExtractRepository extractRepository,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var data = await extractRepository.GetIpaffsCheds(request.From, request.To, cancellationToken);

        return ChedResult(data, httpContext);
    }

    /// <param name="request">The <see cref="DateRangeRequest"/> object</param>
    /// <param name="extractRepository"></param>
    /// <param name="httpContext"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet]
    private static async Task<IResult> TracesCheds(
        [AsParameters] DateRangeRequest request,
        [FromServices] IExtractRepository extractRepository,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var data = await extractRepository.GetTracesCheds(request.From, request.To, cancellationToken);

        return ChedResult(data, httpContext);
    }

    /// <param name="request">The <see cref="DateRangeRequest"/> object</param>
    /// <param name="extractRepository"></param>
    /// <param name="httpContext"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet]
    private static async Task<IResult> CustomsDeclarations(
        [AsParameters] DateRangeRequest request,
        [FromServices] IExtractRepository extractRepository,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var data = await extractRepository.GetCustomsDeclarations(request.From, request.To, cancellationToken);

        return Request.IsCsvRequired(httpContext)
            ? Request.CsvResult(
                data.ToCsvResponse<CustomsDeclarationExtractResponse, CustomsDeclarationExtractResponseMap>()
            )
            : Results.Ok(new DatumResponse<CustomsDeclarationExtractResponse>(data));
    }

    private static IResult ChedResult(IReadOnlyList<ChedExtractResponse> data, HttpContext httpContext) =>
        Request.IsCsvRequired(httpContext)
            ? Request.CsvResult(data.ToCsvResponse<ChedExtractResponse, ChedExtractResponseMap>())
            : Results.Ok(new DatumResponse<ChedExtractResponse>(data));
}
