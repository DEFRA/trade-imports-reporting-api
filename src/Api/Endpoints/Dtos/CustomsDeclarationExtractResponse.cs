using System.Text.Json.Serialization;
using CsvHelper.Configuration;

namespace Defra.TradeImportsReportingApi.Api.Endpoints.Dtos;

public static class ExtractCsv
{
    // ISO 8601 UTC, matching the JSON responses
    public const string DateTimeFormat = "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'";
}

/// <summary>
/// One row per commodity (item number) on a customs declaration. Declarant details are
/// deliberately excluded as they may be personally identifiable.
/// </summary>
public class CustomsDeclarationExtractResponse
{
    [JsonPropertyName("mrn")]
    public required string Mrn { get; init; }

    [JsonPropertyName("mrnCreated")]
    public required DateTime MrnCreated { get; init; }

    [JsonPropertyName("lastUpdated")]
    public required DateTime LastUpdated { get; init; }

    [JsonPropertyName("releaseType")]
    public string? ReleaseType { get; init; }

    [JsonPropertyName("finalState")]
    public string? FinalState { get; init; }

    [JsonPropertyName("dispatchCountryCode")]
    public string? DispatchCountryCode { get; init; }

    [JsonPropertyName("itemNumber")]
    public required int ItemNumber { get; init; }

    [JsonPropertyName("commodityCode")]
    public string? CommodityCode { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("quantityOrWeight")]
    public decimal? QuantityOrWeight { get; init; }

    [JsonPropertyName("chedReferences")]
    public string? ChedReferences { get; init; }

    [JsonPropertyName("authorities")]
    public string? Authorities { get; init; }

    [JsonPropertyName("checkCodes")]
    public string? CheckCodes { get; init; }

    [JsonPropertyName("decisions")]
    public string? Decisions { get; init; }

    [JsonPropertyName("match")]
    public required string Match { get; init; }

    public sealed class CustomsDeclarationExtractResponseMap : ClassMap<CustomsDeclarationExtractResponse>
    {
        public CustomsDeclarationExtractResponseMap()
        {
            Map(m => m.Mrn).Name("MRN");
            Map(m => m.MrnCreated).Name("MRN created").TypeConverterOption.Format(ExtractCsv.DateTimeFormat);
            Map(m => m.LastUpdated).Name("Last updated").TypeConverterOption.Format(ExtractCsv.DateTimeFormat);
            Map(m => m.ReleaseType).Name("Release type");
            Map(m => m.FinalState).Name("Final state");
            Map(m => m.DispatchCountryCode).Name("Dispatch country code");
            Map(m => m.ItemNumber).Name("Item number");
            Map(m => m.CommodityCode).Name("Commodity code");
            Map(m => m.Description);
            Map(m => m.QuantityOrWeight).Name("Quantity/Weight");
            Map(m => m.ChedReferences).Name("CHED references");
            Map(m => m.Authorities);
            Map(m => m.CheckCodes).Name("Check codes");
            Map(m => m.Decisions);
            Map(m => m.Match);
        }
    }
}
