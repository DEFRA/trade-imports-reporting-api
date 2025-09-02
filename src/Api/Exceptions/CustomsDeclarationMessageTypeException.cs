namespace Defra.TradeImportsReportingApi.Api.Exceptions;

public class CustomsDeclarationMessageTypeException(string messageId)
    : Exception($"Customs declaration message with no type received for {messageId}");
