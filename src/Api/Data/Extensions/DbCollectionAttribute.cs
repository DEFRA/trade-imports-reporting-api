namespace Defra.TradeImportsReportingApi.Api.Data.Extensions;

[AttributeUsage(AttributeTargets.Class)]
public class DbCollectionAttribute(string name) : Attribute
{
    public string Name { get; private set; } = name;
}
