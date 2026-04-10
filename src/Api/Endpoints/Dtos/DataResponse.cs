using System.Text.Json.Serialization;
using CsvHelper.Configuration;

namespace Defra.TradeImportsReportingApi.Api.Endpoints.Dtos;

public abstract class DataResponse
{
    [JsonPropertyName("timestamp")]
    public required DateTime Timestamp { get; init; }

    [JsonPropertyName("mrn")]
    public required string Mrn { get; init; }

    [JsonPropertyName("itemNumber")]
    public required int Number { get; init; }

    [JsonPropertyName("commodityCode")]
    public string? CommodityCode { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("quantityOrWeight")]
    public decimal? QuantityOrWeight { get; init; }

    [JsonPropertyName("chedReference")]
    public required string ChedReference { get; init; }

    [JsonPropertyName("match")]
    public required string Match { get; init; }

    [JsonPropertyName("authority")]
    public required string Authority { get; init; }

    [JsonPropertyName("checkCode")]
    public required string CheckCode { get; init; }

    [JsonPropertyName("decision")]
    public string? Decision { get; init; }

    [JsonPropertyName("decisionReasons")]
    public string? DecisionReasons { get; init; }

    public sealed class DataResponseMap : ClassMap<DataResponse>
    {
        public DataResponseMap()
        {
            Map(m => m.Timestamp).Name("Last updated");
            Map(m => m.Mrn).Name("MRN");
            Map(m => m.Number).Name("Item number");
            Map(m => m.CommodityCode).Name("Commodity code");
            Map(m => m.CheckCode).Name("Check code");
            Map(m => m.Description);
            Map(m => m.QuantityOrWeight).Name("Quantity/Weight");
            Map(m => m.ChedReference).Name("CHED reference");
            Map(m => m.Match);
            Map(m => m.Authority);
            Map(m => m.Decision);
            Map(m => m.DecisionReasons).Name("Decision reason");
        }
    }
}
