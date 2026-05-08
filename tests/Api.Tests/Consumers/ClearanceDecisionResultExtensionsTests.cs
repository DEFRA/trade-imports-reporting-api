using System.Text.Json;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsReportingApi.Api.Data.Entities;
using Defra.TradeImportsReportingApi.Api.Data.Extensions;
using Defra.TradeImportsReportingApi.Api.Extensions;

namespace Defra.TradeImportsReportingApi.Api.Tests.Consumers;

public class ClearanceDecisionResultExtensionsTests
{
    private const string ValidDecisionCode = "C03";

    public static TheoryData<string> KnownNoMatchCodes =>
        ["E20", "E30", "E31", "E70", "E71", "E72", "E73", "E75", "E82", "E83", "E87", "E99"];

    public static TheoryData<string> KnownMatchCodes =>
        ["E74", "E80", "E84", "E85", "E86", "E88", "E90", "E92", "E93", "E94", "E95", "E96", "E97"];

    [Fact]
    public void DecisionIsAMatch_WhenNull_ReturnsFalse()
    {
        ClearanceDecisionResult? result = null;

        result.DecisionIsAMatch().Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(DecisionCode.NoMatch)]
    public void DecisionIsAMatch_WhenDecisionCodeIsInvalidOrNullOrNoMatch_ReturnsFalse(string? decisionCode)
    {
        var result = new ClearanceDecisionResult { DecisionCode = decisionCode };

        result.DecisionIsAMatch().Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("X99")]
    public void DecisionIsAMatch_WhenDecisionCodeIsNotNoMatch_AndInternalDecisionCodeIsNullOrWhitespaceOrUnknown_ReturnsTrue(
        string? internalDecisionCode
    )
    {
        var result = new ClearanceDecisionResult
        {
            DecisionCode = ValidDecisionCode,
            InternalDecisionCode = internalDecisionCode,
        };

        result.DecisionIsAMatch().Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(KnownNoMatchCodes))]
    public void DecisionIsAMatch_WhenDecisionCodeIsNotNoMatch_InternalDecisionCodeIsAKnownNoMatchCode_ReturnsFalse(
        string code
    )
    {
        var result = new ClearanceDecisionResult { DecisionCode = ValidDecisionCode, InternalDecisionCode = code };

        result.DecisionIsAMatch().Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(KnownMatchCodes))]
    public void DecisionIsAMatch_WhenDecisionCodeIsMatch_AndKnownMatchCode_ReturnsTrue(string code)
    {
        var result = new ClearanceDecisionResult { DecisionCode = ValidDecisionCode, InternalDecisionCode = code };

        result.DecisionIsAMatch().Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(KnownNoMatchCodes))]
    [MemberData(nameof(KnownMatchCodes))]
    public void InternalDecisionCodeIsUnknown_WhenKnownCode_ReturnsFalse(string code)
    {
        var result = new ClearanceDecisionResult { InternalDecisionCode = code };

        result.InternalDecisionCodeIsUnknown().Should().BeFalse();
    }

    [Fact]
    public void InternalDecisionCodeIsUnknown_WhenUnknownCode_ReturnsTrue()
    {
        var result = new ClearanceDecisionResult { InternalDecisionCode = "X99" };

        result.InternalDecisionCodeIsUnknown().Should().BeTrue();
    }

    [Fact]
    public async Task MapToInternalCustomsDeclaration()
    {
        var json =
            "{\r\n   \"Id\":\"newest\",\r\n   \"Etag\":null,\r\n   \"Created\":\"0001-01-01T00:00:00\",\r\n   \"Updated\":\"0001-01-01T00:00:00\",\r\n   \"ClearanceRequest\": {\r\n        \"externalCorrelationId\": \"CDMS3420021\",\r\n        \"messageSentAt\": \"2026-05-07T08:00:00.001Z\",\r\n        \"externalVersion\": 2,\r\n        \"previousExternalVersion\": 1,\r\n        \"declarationUcr\": \"4GB269573944000-PORTACDMS-342002\",\r\n        \"declarationPartNumber\": null,\r\n        \"declarationType\": \"F\",\r\n        \"arrivesAt\": null,\r\n        \"submitterTurn\": \"GB269573944000\",\r\n        \"declarantId\": \"GB269573944000\",\r\n        \"declarantName\": \"GB269573944000\",\r\n        \"dispatchCountryCode\": \"IT\",\r\n        \"goodsLocationCode\": \"DEUDEUDEUGVM\",\r\n        \"masterUcr\": null,\r\n        \"commodities\": [\r\n            {\r\n                \"itemNumber\": 1,\r\n                \"customsProcedureCode\": \"40001CG\",\r\n                \"taricCommodityCode\": \"1601009105\",\r\n                \"goodsDescription\": \"1A -24GBBGBKCDMS342002\",\r\n                \"consigneeId\": \"GB930101485000\",\r\n                \"consigneeName\": \"GB930101485000\",\r\n                \"netMass\": 50,\r\n                \"supplementaryUnits\": 175,\r\n                \"thirdQuantity\": null,\r\n                \"originCountryCode\": \"IT\",\r\n                \"documents\": [\r\n                    {\r\n                        \"documentCode\": \"C640\",\r\n                        \"documentReference\": \"GBCHD2026.1342002\",\r\n                        \"documentStatus\": \"AE\",\r\n                        \"documentControl\": \"P\",\r\n                        \"documentQuantity\": null\r\n                    },\r\n                    {\r\n                        \"documentCode\": \"C640\",\r\n                        \"documentReference\": \"GBCHD2026.2342002\",\r\n                        \"documentStatus\": \"AE\",\r\n                        \"documentControl\": \"P\",\r\n                        \"documentQuantity\": null\r\n                    }\r\n                ],\r\n                \"checks\": [\r\n                    {\r\n                        \"checkCode\": \"H221\",\r\n                        \"departmentCode\": \"AHVLA\"\r\n                    }\r\n                ]\r\n            },\r\n            {\r\n                \"itemNumber\": 2,\r\n                \"customsProcedureCode\": \"40001CG\",\r\n                \"taricCommodityCode\": \"1601009105\",\r\n                \"goodsDescription\": \"2A -24GBBGBKCDMS342002\",\r\n                \"consigneeId\": \"GB930101485000\",\r\n                \"consigneeName\": \"GB930101485000\",\r\n                \"netMass\": 50,\r\n                \"supplementaryUnits\": 175,\r\n                \"thirdQuantity\": null,\r\n                \"originCountryCode\": \"IT\",\r\n                \"documents\": [\r\n                    {\r\n                        \"documentCode\": \"C640\",\r\n                        \"documentReference\": \"GBCHD2026.3342002\",\r\n                        \"documentStatus\": \"AE\",\r\n                        \"documentControl\": \"P\",\r\n                        \"documentQuantity\": null\r\n                    }\r\n                ],\r\n                \"checks\": [\r\n                    {\r\n                        \"checkCode\": \"H221\",\r\n                        \"departmentCode\": \"AHVLA\"\r\n                    }\r\n                ]\r\n            }\r\n        ]\r\n    },\r\n    \"ClearanceDecision\": {\r\n        \"correlationId\": \"17781705974829123569\",\r\n        \"created\": \"2026-05-07T16:16:37.482Z\",\r\n        \"externalVersionNumber\": 2,\r\n        \"decisionNumber\": 2,\r\n        \"sourceVersion\": null,\r\n        \"items\": [\r\n            {\r\n                \"itemNumber\": 1,\r\n                \"checks\": [\r\n                    {\r\n                        \"checkCode\": \"H221\",\r\n                        \"decisionCode\": \"X00\",\r\n                        \"decisionsValidUntil\": null,\r\n                        \"decisionReasons\": [\r\n                            \"A Customs Declaration has been submitted however no matching CVEDA(s) have been submitted to Animal Health for CVEDA number(s) GBCHD2026.2342002.\"\r\n                        ],\r\n                        \"decisionInternalFurtherDetail\": [\r\n                            \"E70\"\r\n                        ]\r\n                    }\r\n                ]\r\n            },\r\n            {\r\n                \"itemNumber\": 2,\r\n                \"checks\": [\r\n                    {\r\n                        \"checkCode\": \"H221\",\r\n                        \"decisionCode\": \"X00\",\r\n                        \"decisionsValidUntil\": null,\r\n                        \"decisionReasons\": [\r\n                            \"A Customs Declaration has been submitted however no matching CVEDA(s) have been submitted to Animal Health for CVEDA number(s) GBCHD2026.3342002.\"\r\n                        ],\r\n                        \"decisionInternalFurtherDetail\": [\r\n                            \"E70\"\r\n                        ]\r\n                    }\r\n                ]\r\n            }\r\n        ],\r\n        \"results\": [\r\n            {\r\n                \"itemNumber\": 1,\r\n                \"importPreNotification\": \"CHEDA.GB.2026.1342002\",\r\n                \"documentReference\": \"GBCHD2026.1342002\",\r\n                \"documentCode\": \"C640\",\r\n                \"checkCode\": \"H221\",\r\n                \"decisionCode\": \"H01\",\r\n                \"decisionReason\": null,\r\n                \"internalDecisionCode\": null,\r\n                \"mode\": \"Active\",\r\n                \"level\": 1,\r\n                \"ruleName\": \"InspectionRequiredDecisionRule\"\r\n            },\r\n            {\r\n                \"itemNumber\": 1,\r\n                \"importPreNotification\": \"CHEDA.GB.2026.1342002\",\r\n                \"documentReference\": \"GBCHD2026.1342002\",\r\n                \"documentCode\": \"C640\",\r\n                \"checkCode\": \"H221\",\r\n                \"decisionCode\": \"X00\",\r\n                \"decisionReason\": null,\r\n                \"internalDecisionCode\": \"E20\",\r\n                \"mode\": \"Passive\",\r\n                \"level\": 2,\r\n                \"ruleName\": \"CommodityCodeDecisionRule\"\r\n            },\r\n            {\r\n                \"itemNumber\": 1,\r\n                \"importPreNotification\": null,\r\n                \"documentReference\": \"GBCHD2026.2342002\",\r\n                \"documentCode\": \"C640\",\r\n                \"checkCode\": \"H221\",\r\n                \"decisionCode\": \"X00\",\r\n                \"decisionReason\": \"CHED reference GBCHD2026.2342002 cannot be found in IPAFFS. Check that the reference is correct.\",\r\n                \"internalDecisionCode\": \"E70\",\r\n                \"mode\": \"Active\",\r\n                \"level\": 1,\r\n                \"ruleName\": \"UnlinkedNotificationDecisionRule\"\r\n            },\r\n            {\r\n                \"itemNumber\": 2,\r\n                \"importPreNotification\": null,\r\n                \"documentReference\": \"GBCHD2026.3342002\",\r\n                \"documentCode\": \"C640\",\r\n                \"checkCode\": \"H221\",\r\n                \"decisionCode\": \"X00\",\r\n                \"decisionReason\": \"CHED reference GBCHD2026.3342002 cannot be found in IPAFFS. Check that the reference is correct.\",\r\n                \"internalDecisionCode\": \"E70\",\r\n                \"mode\": \"Active\",\r\n                \"level\": 1,\r\n                \"ruleName\": \"UnlinkedNotificationDecisionRule\"\r\n            }\r\n        ]\r\n    },\r\n    \"finalisation\": null,\r\n    \"externalErrors\": null\r\n}";
        var @event = JsonSerializer.Deserialize<CustomsDeclarationEvent>(json);

        var cd = @event?.ToCustomsDeclaration();

        await Verify(cd).UseStrictJson();
    }
}
