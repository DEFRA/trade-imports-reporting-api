using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsReportingApi.Api.Data.Entities;
using Defra.TradeImportsReportingApi.Api.Extensions;
using CustomsDeclaration = Defra.TradeImportsReportingApi.Api.Data.Entities.CustomsDeclaration;

namespace Defra.TradeImportsReportingApi.Api.Data.Extensions;

public static class CustomsDeclarationExtensions
{
    private const string CancelledAfterArrival = "1";
    private const string CancelledWhilePreLodged = "2";

    public static CustomsDeclaration ToCustomsDeclaration(this CustomsDeclarationEvent customsDeclarationEvent)
    {
        var cd = new CustomsDeclaration
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
            Match = customsDeclarationEvent.ClearanceDecision?.Results?.All(x => x.DecisionIsAMatch()) ?? false,
            MatchLevel1 =
                customsDeclarationEvent
                    .ClearanceDecision?.Results?.Where(x => x.Level == 1)
                    .All(x => x.DecisionIsAMatch()) ?? false,
        };

        if (cd.MatchLevel1 == true)
        {
            cd.MatchLevel2 = customsDeclarationEvent.ClearanceDecision?.Results?.All(x => x.Level != 2) ?? false;
        }

        if (cd.MatchLevel2 == true)
        {
            cd.MatchLevel3 = customsDeclarationEvent.ClearanceDecision?.Results?.All(x => x.Level != 3) ?? false;
        }

        return cd;
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
                        var decisionResults =
                            customsDeclarationEvent.ClearanceDecision?.Results?.Where(result =>
                                result.ItemNumber == commodity.ItemNumber
                            ) ?? [];

                        foreach (var decisionResult in decisionResults)
                        {
                            yield return new CustomsDeclarationItem
                            {
                                Number = commodity.ItemNumber.GetValueOrDefault(),
                                CommodityCode = commodity.TaricCommodityCode!,
                                Description = commodity.GoodsDescription,
                                Match = decisionResult?.DecisionIsAMatch() ?? false,
                                ChedReference = document.DocumentReference!.Value,
                                Authority = check.DepartmentCode!,
                                Decision = decisionResult?.DecisionCode,
                                DecisionReasons = decision.DecisionReasons,
                                QuantityOrWeight = commodity.SupplementaryUnits ?? commodity.NetMass,
                                CheckCode = decisionResult?.CheckCode!,
                                Mode = decisionResult?.Mode,
                                MatchLevel = decisionResult?.Level,
                                RuleName = decisionResult?.RuleName,
                            };
                        }
                    }
                }
            }
        }
    }
}
