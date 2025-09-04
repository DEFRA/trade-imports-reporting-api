using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;

namespace Defra.TradeImportsReportingApi.Api.Models;

// ReSharper disable once ClassNeverInstantiated.Global
public class CustomsDeclarationEntity : CustomsDeclaration
{
    public DateTime Created { get; init; }
}
