using System.ComponentModel.DataAnnotations;

namespace Defra.TradeImportsReportingApi.Api.Configuration;

public class ResourceEventsConsumerOptions
{
    public const string SectionName = "ResourceEventsConsumerOptions";

    [Required]
    public required bool AutoStartConsumers { get; init; }

    [Required]
    public required string QueueName { get; init; }

    public int ConsumersPerHost { get; init; } = 20;
}
