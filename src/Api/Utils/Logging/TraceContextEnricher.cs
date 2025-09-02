using Serilog.Core;
using Serilog.Events;

namespace Defra.TradeImportsReportingApi.Api.Utils.Logging;

public class TraceContextEnricher : ILogEventEnricher
{
    private const string PropertyName = "CorrelationId";
    private readonly ITraceContextAccessor _traceContextAccessor;

    public TraceContextEnricher()
        : this(new TraceContextAccessor()) { }

    private TraceContextEnricher(ITraceContextAccessor traceContextAccessor)
    {
        _traceContextAccessor = traceContextAccessor;
    }

    /// <inheritdoc/>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var requestContext = _traceContextAccessor.Context;
        if (requestContext == null)
            return;

        logEvent.AddOrUpdateProperty(new LogEventProperty(PropertyName, new ScalarValue(requestContext.TraceId)));
    }
}
