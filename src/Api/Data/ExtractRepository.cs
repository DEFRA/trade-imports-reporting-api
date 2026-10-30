using System.Diagnostics.CodeAnalysis;
using Defra.TradeImportsReportingApi.Api.Data.Extensions;
using Defra.TradeImportsReportingApi.Api.Endpoints.Dtos;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Defra.TradeImportsReportingApi.Api.Data;

/// <summary>
/// Extracts read from the snapshot collections and select a snapshot when it was last
/// updated within [from, to).
/// </summary>
[ExcludeFromCodeCoverage]
public class ExtractRepository(IDbContext dbContext) : IExtractRepository
{
    public async Task<IReadOnlyList<ChedExtractResponse>> GetIpaffsCheds(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken
    )
    {
        GuardUtc(from, to);

        var notifications = await dbContext
            .ImportPreNotifications.AsQueryable()
            .Where(x => x.Updated >= from && x.Updated < to)
            .OrderByDescending(x => x.Updated)
            .ToListAsync(cancellationToken);

        return notifications.SelectMany(x => x.ToExtractRows()).ToList();
    }

    public async Task<IReadOnlyList<ChedExtractResponse>> GetTracesCheds(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken
    )
    {
        GuardUtc(from, to);

        var cheds = await dbContext
            .TracesCheds.AsQueryable()
            .Where(x => x.Updated >= from && x.Updated < to)
            .OrderByDescending(x => x.Updated)
            .ToListAsync(cancellationToken);

        return cheds.SelectMany(x => x.ToExtractRows()).ToList();
    }

    public async Task<IReadOnlyList<CustomsDeclarationExtractResponse>> GetCustomsDeclarations(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken
    )
    {
        GuardUtc(from, to);

        var customsDeclarations = await dbContext
            .CustomsDeclarations.AsQueryable()
            .Where(x => x.LastUpdated >= from && x.LastUpdated < to)
            .OrderByDescending(x => x.LastUpdated)
            .ToListAsync(cancellationToken);

        return customsDeclarations.SelectMany(x => x.ToExtractRows()).ToList();
    }

    private static void GuardUtc(DateTime from, DateTime to)
    {
        if (from.Kind != DateTimeKind.Utc)
            throw new ArgumentOutOfRangeException(nameof(from), from, "From must be UTC");

        if (to.Kind != DateTimeKind.Utc)
            throw new ArgumentOutOfRangeException(nameof(to), to, "To must be UTC");
    }
}
