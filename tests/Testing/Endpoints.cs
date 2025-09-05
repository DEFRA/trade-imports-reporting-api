namespace Defra.TradeImportsReportingApi.Testing;

public static class Endpoints
{
    public static class ReleasesSummary
    {
        private const string Root = "/releases/summary";

        public static string Get(EndpointQuery? query = null) => $"{Root}/{query}";
    }

    public static class MatchesSummary
    {
        private const string Root = "/matches/summary";

        public static string Get(EndpointQuery? query = null) => $"{Root}/{query}";
    }

    public static class ClearanceRequestsSummary
    {
        private const string Root = "/clearance-requests/summary";

        public static string Get(EndpointQuery? query = null) => $"{Root}/{query}";
    }

    public static class Summary
    {
        private const string Root = "/summary";

        public static string Get(EndpointQuery? query = null) => $"{Root}/{query}";
    }
}
