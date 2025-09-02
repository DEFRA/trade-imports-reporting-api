using System.ComponentModel.DataAnnotations;

namespace Defra.TradeImportsReportingApi.Api.Utils.Logging;

public class TraceHeader
{
    [ConfigurationKeyName("TraceHeader")]
    [Required]
    public required string Name { get; set; }
}
