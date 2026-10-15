using System.ComponentModel.DataAnnotations;

namespace Defra.TradeImportsReportingApi.Api.Configuration;

public class TracesChedsResourceEventsConsumerOptions
{
    public const string SectionName = "TracesChedsResourceEventsConsumerOptions";

    [Required]
    public required bool AutoStartConsumers { get; init; }

    [Required]
    public required string QueueName { get; init; }

    public string DeadLetterQueueName => $"{QueueName}-deadletter";

    public int ConsumersPerHost { get; init; } = 20;
}
