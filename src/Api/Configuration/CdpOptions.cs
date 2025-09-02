namespace Defra.TradeImportsReportingApi.Api.Configuration;

public class CdpOptions
{
    [ConfigurationKeyName("CDP_HTTPS_PROXY")]
    public string? CdpHttpsProxy { get; init; }

    public bool IsProxyEnabled => !string.IsNullOrWhiteSpace(CdpHttpsProxy);
}
