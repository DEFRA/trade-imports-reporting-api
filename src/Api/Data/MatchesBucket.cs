namespace Defra.TradeImportsReportingApi.Api.Data;

public record MatchesBucket(DateTime Bucket, MatchesSummary Summary) : IBucket;
