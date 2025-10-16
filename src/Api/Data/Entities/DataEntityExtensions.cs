using System.Collections.Concurrent;
using Defra.TradeImportsReportingApi.Api.Data.Extensions;

namespace Defra.TradeImportsReportingApi.Api.Data.Entities;

public static class DataEntityExtensions
{
    private static readonly ConcurrentDictionary<Type, string> s_attributeCache = new();

    public static string DataEntityName(this Type type)
    {
        return s_attributeCache.GetOrAdd(
            type,
            t =>
                t.GetCustomAttributes(typeof(DbCollectionAttribute), false).FirstOrDefault()
                    is DbCollectionAttribute attribute
                    ? attribute.Name
                    : t.Name.Replace("Entity", "")
        );
    }
}
