namespace Defra.TradeImportsReportingApi.Api.Exceptions;

public class ResourceEventException(string messageId)
    : Exception($"Invalid resource event message received for {messageId}") { }
