namespace Defra.TradeImportsReportingApi.Api.Data;

public class CheckCode
{
    public required string Value { get; set; }

    public bool IsValidDocumentCode(string? documentCode)
    {
        if (string.IsNullOrEmpty(documentCode))
        {
            return false;
        }

        return Value switch
        {
            "H218" or "H220" => documentCode is "C085" or "N002",
            "H219" => documentCode is "C085" or "9115" or "N851",
            "H221" => documentCode is "C640",
            "H222" or "H224" => documentCode is "N853",
            "H223" => documentCode is "C678" or "N852",
            _ => false,
        };
    }

    public override string ToString() => Value;
}
