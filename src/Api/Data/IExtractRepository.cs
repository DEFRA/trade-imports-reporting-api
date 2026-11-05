using Defra.TradeImportsReportingApi.Api.Endpoints.Dtos;

namespace Defra.TradeImportsReportingApi.Api.Data;

public interface IExtractRepository
{
    Task<IReadOnlyList<ChedExtractResponse>> GetIpaffsCheds(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<ChedExtractResponse>> GetTracesCheds(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<CustomsDeclarationExtractResponse>> GetCustomsDeclarations(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken
    );
}
