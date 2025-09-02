using System.Linq.Expressions;

namespace Defra.TradeImportsReportingApi.Api.Data;

public interface IFieldUpdateBuilder<T>
{
    IFieldUpdateBuilder<T> Set<TField>(Expression<Func<T, TField>> field, TField value);
}
