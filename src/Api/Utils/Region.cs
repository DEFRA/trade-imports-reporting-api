namespace Defra.TradeImportsReportingApi.Api.Utils;

public static class Region
{
    private static readonly string[] s_eu =
    [
        "AT",
        "BE",
        "BG",
        "HR",
        "CY",
        "CZ",
        "DK",
        "EE",
        "FI",
        "FR",
        "DE",
        "GR",
        "HU",
        "IS",
        "IE",
        "IT",
        "LV",
        "LI",
        "LT",
        "LU",
        "MT",
        "NL",
        "NO",
        "PL",
        "PT",
        "RO",
        "SK",
        "SI",
        "ES",
        "SE",
        "CH",
    ];

    public static bool IsEu(string? region) => s_eu.Contains(region);
}
