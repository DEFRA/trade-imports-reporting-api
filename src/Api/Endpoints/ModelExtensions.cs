using System.Text.Json;

namespace Defra.TradeImportsReportingApi.Api.Endpoints
{
    public static class ModelExtensions
    {
        public static string ToCamelCase(this string propertyName)
        {
            return JsonNamingPolicy.CamelCase.ConvertName(propertyName);
        }
    }
}
