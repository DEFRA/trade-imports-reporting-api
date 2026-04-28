using Defra.TradeImportsReportingApi.Api.Data.Extensions;

namespace Defra.TradeImportsReportingApi.Api.Data.Entities;

[DbCollection("CustomsDeclaration")]
public class CustomsDeclaration
{
    public required string Id { get; init; }
    public required DateTime MrnCreated { get; init; }
    public required DateTime Timestamp { get; init; }
    public bool? Match { get; set; }
    public bool? MatchLevel1 { get; set; }
    public bool? MatchLevel2 { get; set; }
    public bool? MatchLevel3 { get; set; }
    public string? ReleaseType { get; set; }
    public CustomsDeclarationItem[] Items { get; set; } = [];
}

public class CustomsDeclarationItem
{
    public required int Number { get; init; }
    public string? CommodityCode { get; init; }
    public string? Description { get; init; }
    public decimal? QuantityOrWeight { get; init; }
    public required string ChedReference { get; set; }
    public bool? Match { get; set; }
    public required string Authority { get; set; }
    public required string CheckCode { get; set; }
    public string? Decision { get; set; }
    public string[]? DecisionReasons { get; set; } = [];
    public string? Mode { get; set; }
    public int? MatchLevel { get; set; }
    public string? RuleName { get; set; }
}
