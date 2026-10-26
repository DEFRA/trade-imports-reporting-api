// ReSharper disable InconsistentNaming
namespace Defra.TradeImportsReportingApi.Api.Models;

public static class ImportNotificationStatus
{
    public const string Submitted = "SUBMITTED";
    public const string Validated = "VALIDATED";
    public const string InProgress = "IN_PROGRESS";
    public const string Replaced = "REPLACED";
    public const string Cancelled = "CANCELLED";
    public const string Deleted = "DELETED";
    public const string SplitConsignment = "SPLIT_CONSIGNMENT";
}

public static class TracesNotificationStatus
{
    public const string Submitted = "1";
    public const string Validated = "70";
    public const string InProgress = "42";
    public const string Replaced = "44";
    public const string Cancelled = "64";
    public const string Deleted = "55";
    public const string SplitConsignment = "68";
}

public static class TracesChedType
{
    public const string CHEDA = nameof(CHEDA);
    public const string CHEDP = nameof(CHEDP);
    public const string CHEDPP = nameof(CHEDPP);
    public const string CHEDD = nameof(CHEDD);
}
