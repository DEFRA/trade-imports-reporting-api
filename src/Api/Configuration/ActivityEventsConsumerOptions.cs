using System.ComponentModel.DataAnnotations;

namespace Defra.TradeImportsReportingApi.Api.Configuration;

public class ActivityEventsConsumerOptions
{
    public const string SectionName = "ActivityEventsConsumerOptions";

    [Required]
    public required bool AutoStartConsumers { get; init; }

    [Required]
    public required string QueueName { get; init; }

    public string DeadLetterQueueName => $"{QueueName}-deadletter";

    public int ConsumersPerHost { get; init; } = 20;
}
