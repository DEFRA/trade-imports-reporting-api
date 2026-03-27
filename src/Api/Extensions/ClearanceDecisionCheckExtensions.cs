using System.Runtime.CompilerServices;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsReportingApi.Api.Data.Entities;

namespace Defra.TradeImportsReportingApi.Api.Extensions
{
    public static class ClearanceDecisionResultExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool DecisionIsAMatch(this ClearanceDecisionResult? clearanceDecisionResult)
        {
            if (string.IsNullOrWhiteSpace(clearanceDecisionResult?.DecisionCode))
            {
                return false;
            }

            return clearanceDecisionResult.DecisionCode is not DecisionCode.NoMatch
                && (
                    clearanceDecisionResult.InternalDecisionCode == null
                    || !IsKnownNoMatchCode(clearanceDecisionResult.InternalDecisionCode)
                );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool InternalDecisionCodeIsUnknown(this ClearanceDecisionResult clearanceDecisionResult)
        {
            if (string.IsNullOrEmpty(clearanceDecisionResult.InternalDecisionCode))
            {
                return false;
            }

            return !(
                IsKnownNoMatchCode(clearanceDecisionResult.InternalDecisionCode)
                || IsKnownMatchCode(clearanceDecisionResult.InternalDecisionCode)
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsKnownNoMatchCode(string matchCode) =>
            matchCode
                is "E20"
                    or "E30"
                    or "E31"
                    or "E70"
                    or "E71"
                    or "E72"
                    or "E73"
                    or "E75"
                    or "E82"
                    or "E83"
                    or "E87"
                    or "E99";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsKnownMatchCode(string matchCode) =>
            matchCode
                is "E74"
                    or "E80"
                    or "E84"
                    or "E85"
                    or "E86"
                    or "E88"
                    or "E90"
                    or "E92"
                    or "E93"
                    or "E94"
                    or "E95"
                    or "E96"
                    or "E97";
    }
}
