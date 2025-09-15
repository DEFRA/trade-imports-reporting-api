namespace Defra.TradeImportsReportingApi.Api.Data;

public record ReleasesBucket(DateTime Bucket, ReleasesSummary Summary) : IBucket;
