namespace Defra.TradeImportsReportingApi.Testing;

public static class Endpoints
{
    public static class ReleasesSummary
    {
        private const string Root = "/releases/summary";

        public static string Get(EndpointQuery? query = null) => $"{Root}/{query}";
    }
}
