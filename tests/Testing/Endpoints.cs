using System.Diagnostics.CodeAnalysis;

// ReSharper disable MemberHidesStaticFromOuterClass

namespace Defra.TradeImportsReportingApi.Testing;

[SuppressMessage(
    "Critical Code Smell",
    "S3218:Inner class members should not shadow outer class \"static\" or type members",
    Justification = "Test class only, therefore scope of risk is acceptable"
)]
public static class Endpoints
{
    public static class Releases
    {
        private const string Root = "/releases";

        public static string Summary(EndpointQuery? query = null) => $"{Root}/summary{query}";

        public static string Buckets(EndpointQuery? query = null) => $"{Root}/buckets{query}";
    }

    public static class Matches
    {
        private const string Root = "/matches";

        public static string Summary(EndpointQuery? query = null) => $"{Root}/summary{query}";

        public static string Buckets(EndpointQuery? query = null) => $"{Root}/buckets{query}";
    }

    public static class ClearanceRequests
    {
        private const string Root = "/clearance-requests";

        public static string Summary(EndpointQuery? query = null) => $"{Root}/summary{query}";

        public static string Buckets(EndpointQuery? query = null) => $"{Root}/buckets{query}";
    }

    public static class NotificationsSummary
    {
        private const string Root = "/notifications/summary";

        public static string Get(EndpointQuery? query = null) => $"{Root}/{query}";
    }

    public static class Summary
    {
        private const string Root = "/summary";

        public static string Get(EndpointQuery? query = null) => $"{Root}/{query}";
    }

    public static class LastReceived
    {
        private const string Root = "/last-received";

        public static string Get() => $"{Root}";
    }
}
