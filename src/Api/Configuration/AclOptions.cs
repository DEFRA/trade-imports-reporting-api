using System.ComponentModel.DataAnnotations;

namespace Defra.TradeImportsReportingApi.Api.Configuration;

public class AclOptions
{
    public Dictionary<string, Client> Clients { get; init; } = new();

    public class Client
    {
        [Required]
        public required string Secret { get; init; } = string.Empty;
    }
}
