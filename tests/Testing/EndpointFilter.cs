namespace Defra.TradeImportsReportingApi.Testing;

public class EndpointFilter
{
    internal string Filter { get; }

    private EndpointFilter(string filter) => Filter = filter;

    public static EndpointFilter From(DateTime from) => new($"from={from:O}");

    public static EndpointFilter To(DateTime to) => new($"to={to:O}");

    public static EndpointFilter Unit(string unit) => new($"unit={unit}");

    public static EndpointFilter Match(bool match) => new($"match={match.ToString().ToLower()}");

    public static EndpointFilter ReleaseType(string releaseType) => new($"releaseType={releaseType}");
}
