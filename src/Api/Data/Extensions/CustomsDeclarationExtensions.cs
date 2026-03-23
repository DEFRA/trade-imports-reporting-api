using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsReportingApi.Api.Data.Entities;

namespace Defra.TradeImportsReportingApi.Api.Data.Extensions;

public static class CustomsDeclarationExtensions
{
    private const string CancelledAfterArrival = "1";
    private const string CancelledWhilePreLodged = "2";

    public static CustomsDeclaration ToCustomsDeclaration(this CustomsDeclarationEvent customsDeclarationEvent)
    {
        return new CustomsDeclaration
        {
            Id = customsDeclarationEvent.Id,
            MrnCreated = customsDeclarationEvent.ClearanceRequest?.MessageSentAt ?? DateTime.MinValue,
            Timestamp = customsDeclarationEvent.Created,
            ReleaseType = customsDeclarationEvent.Finalisation switch
            {
                { FinalState: CancelledAfterArrival or CancelledWhilePreLodged } => ReleaseType.Cancelled,
                { IsManualRelease: false, FinalState: not CancelledAfterArrival and not CancelledWhilePreLodged } =>
                    ReleaseType.Automatic,
                { IsManualRelease: true } => ReleaseType.Manual,
                _ => ReleaseType.Unknown,
            },
            Items = customsDeclarationEvent.ToCustomsDeclarationItem().ToArray(),
            Match = customsDeclarationEvent.ClearanceDecision?.Items.All(x =>
                x.Checks.All(y => y.DecisionCode is not DecisionCode.NoMatch)
            ),
        };
    }

    private static IEnumerable<CustomsDeclarationItem> ToCustomsDeclarationItem(
        this CustomsDeclarationEvent customsDeclarationEvent
    )
    {
        var commodities = customsDeclarationEvent.ClearanceRequest?.Commodities ?? [];
        foreach (var commodity in commodities)
        {
            var documents = commodity.Documents ?? [];
            foreach (var document in documents)
            {
                var match =
                    customsDeclarationEvent.ClearanceDecision != null
                    && customsDeclarationEvent
                        .ClearanceDecision.Items.Where(x => x.ItemNumber == commodity.ItemNumber)
                        .All(x => x.Checks.All(y => y.DecisionCode is not DecisionCode.NoMatch));

                if (commodity.Checks == null)
                    yield break;

                var checks = commodity
                    .Checks.Where(x =>
                        new CheckCode() { Value = x.CheckCode! }.IsValidDocumentCode(document.DocumentCode)
                    )
                    .ToArray();

                foreach (var check in checks)
                {
                    var decisions =
                        customsDeclarationEvent
                            .ClearanceDecision?.Items.Where(x => x.ItemNumber == commodity.ItemNumber)
                            .SelectMany(x => x.Checks)
                            .Where(x => x.CheckCode == check.CheckCode)
                            .Select(x => x)
                            .ToArray() ?? [];

                    foreach (var decision in decisions)
                    {
                        yield return new CustomsDeclarationItem
                        {
                            Number = commodity.ItemNumber.GetValueOrDefault(),
                            CommodityCode = commodity.TaricCommodityCode!,
                            Description = commodity.GoodsDescription,
                            Match = match,
                            ChedReference = document.DocumentReference!.Value,
                            Authority = check.DepartmentCode!,
                            Decision = decision.DecisionCode,
                            DecisionReasons = decision.DecisionReasons,
                            QuantityOrWeight = commodity.SupplementaryUnits ?? commodity.NetMass,
                            CheckCode = decision.CheckCode,
                        };
                    }
                }
            }
        }
    }
}
