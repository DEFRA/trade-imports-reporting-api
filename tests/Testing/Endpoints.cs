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

        public static string Intervals(EndpointQuery? query = null) => $"{Root}/intervals{query}";

        public static string Data(EndpointQuery? query = null) => $"{Root}/data{query}";
    }

    public static class Matches
    {
        private const string Root = "/matches";

        public static string Summary(EndpointQuery? query = null) => $"{Root}/summary{query}";

        public static string Buckets(EndpointQuery? query = null) => $"{Root}/buckets{query}";

        public static string Intervals(EndpointQuery? query = null) => $"{Root}/intervals{query}";

        public static string Data(EndpointQuery? query = null) => $"{Root}/data{query}";
    }

    public static class ClearanceRequests
    {
        private const string Root = "/clearance-requests";

        public static string Summary(EndpointQuery? query = null) => $"{Root}/summary{query}";

        public static string Buckets(EndpointQuery? query = null) => $"{Root}/buckets{query}";

        public static string Intervals(EndpointQuery? query = null) => $"{Root}/intervals{query}";
    }

    public static class Notifications
    {
        private const string Root = "/notifications";

        public static string Summary(EndpointQuery? query = null) => $"{Root}/summary{query}";

        public static string Buckets(EndpointQuery? query = null) => $"{Root}/buckets{query}";

        public static string Intervals(EndpointQuery? query = null) => $"{Root}/intervals{query}";
    }

    public static class Summary
    {
        private const string Root = "/summary";

        public static string Get(EndpointQuery? query = null) => $"{Root}/{query}";
    }

    public static class Buckets
    {
        private const string Root = "/buckets";

        public static string Get(EndpointQuery? query = null) => $"{Root}/{query}";
    }

    public static class Intervals
    {
        private const string Root = "/intervals";

        public static string Get(EndpointQuery? query = null) => $"{Root}/{query}";
    }

    public static class LastReceived
    {
        private const string Root = "/last-received";

        public static string Get() => $"{Root}";
    }

    public static class LastSent
    {
        private const string Root = "/last-sent";

        public static string Get() => $"{Root}";
    }

    public static class Status
    {
        private const string Root = "/status";

        public static string Get() => $"{Root}";
    }

    public static class Admin
    {
        private const string Root = "/admin";

        public static class DeadLetterQueue
        {
            private const string SubRoot = $"{Root}/dlq";

            public static string Redrive() => $"{SubRoot}/redrive";

            public static string RemoveMessage(string? messageId = null) =>
                $"{SubRoot}/remove-message?messageId={messageId}";

            public static string Drain() => $"{SubRoot}/drain";
        }
    }
}
