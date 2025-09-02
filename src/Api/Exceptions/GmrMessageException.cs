namespace Defra.TradeImportsReportingApi.Api.Exceptions;

public class GmrMessageException(string messageId) : Exception($"Invalid GMR message received for {messageId}");
