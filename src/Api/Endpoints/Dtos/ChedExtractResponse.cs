using System.Text.Json.Serialization;
using CsvHelper.Configuration;

namespace Defra.TradeImportsReportingApi.Api.Endpoints.Dtos;

/// <summary>
/// One row per commodity on a CHED, shared by the IPAFFS and TRACES extracts.
/// </summary>
public class ChedExtractResponse
{
    [JsonPropertyName("chedReference")]
    public required string ChedReference { get; init; }

    [JsonPropertyName("chedType")]
    public required string ChedType { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("created")]
    public required DateTime Created { get; init; }

    [JsonPropertyName("updated")]
    public required DateTime Updated { get; init; }

    [JsonPropertyName("commodityCode")]
    public string? CommodityCode { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    public sealed class ChedExtractResponseMap : ClassMap<ChedExtractResponse>
    {
        public ChedExtractResponseMap()
        {
            Map(m => m.ChedReference).Name("CHED reference");
            Map(m => m.ChedType).Name("CHED type");
            Map(m => m.Status);
            Map(m => m.Created).TypeConverterOption.Format(ExtractCsv.DateTimeFormat);
            Map(m => m.Updated).Name("Last updated").TypeConverterOption.Format(ExtractCsv.DateTimeFormat);
            Map(m => m.CommodityCode).Name("Commodity code");
            Map(m => m.Description);
        }
    }
}
