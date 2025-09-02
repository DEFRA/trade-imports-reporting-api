using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using Amazon.CloudWatch.EMF.Logger;
using Amazon.CloudWatch.EMF.Model;
using Microsoft.Extensions.Logging.Abstractions;
using Unit = Amazon.CloudWatch.EMF.Model.Unit;

// ReSharper disable InconsistentNaming

namespace Defra.TradeImportsReportingApi.Api.Metrics;

[ExcludeFromCodeCoverage]
public static class EmfExporter
{
    private static readonly MeterListener s_meterListener = new();
    private static ILogger _logger = null!;
    private static ILoggerFactory s_loggerFactory = NullLoggerFactory.Instance;
    private static string? s_awsNamespace;

    public static void Init(ILoggerFactory loggerFactory, string? awsNamespace)
    {
        _logger = loggerFactory.CreateLogger(nameof(EmfExporter));
        s_loggerFactory = loggerFactory;
        s_awsNamespace = awsNamespace;

        s_meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name is MetricsConstants.MetricNames.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        s_meterListener.SetMeasurementEventCallback<int>(OnMeasurementRecorded);
        s_meterListener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
        s_meterListener.SetMeasurementEventCallback<double>(OnMeasurementRecorded);
        s_meterListener.Start();
    }

    private static void OnMeasurementRecorded<T>(
        Instrument instrument,
        T measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        object? state
    )
    {
        try
        {
            using var metricsLogger = new MetricsLogger(s_loggerFactory);

            metricsLogger.SetNamespace(s_awsNamespace);
            var dimensionSet = new DimensionSet();

            foreach (var tag in tags)
            {
                if (string.IsNullOrWhiteSpace(tag.Value?.ToString()))
                    continue;

                dimensionSet.AddDimension(tag.Key, tag.Value?.ToString());
            }

            metricsLogger.SetDimensions(dimensionSet);
            var name = instrument.Name;

            metricsLogger.PutMetric(name, Convert.ToDouble(measurement), Enum.Parse<Unit>(instrument.Unit!));
            metricsLogger.Flush();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to push EMF metric");
        }
    }
}
