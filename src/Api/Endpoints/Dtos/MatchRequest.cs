using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Defra.TradeImportsReportingApi.Api.Endpoints.Dtos
{
    public sealed class MatchRequest : DateRangeRequest
    {
        /// <summary>Whether to return items that have been matched or not matched</summary>
        /// <example>true</example>
        [Required]
        [FromQuery(Name = "match")]
        public bool Match { get; set; }

        /// <summary>The match level to filter on (1, 2 or 3)</summary>
        /// <example>1</example>
        /// <remarks>Only valid if <see cref="Match"/>Match is set to true</remarks>
        [AllowedValues(null, 1, 2, 3, ErrorMessage = "matchLevel must be 1 or 2 or 3 if specified")]
        [FromQuery(Name = "matchLevel")]
        public int? MatchLevel { get; set; } = null;

        /// <summary>Whether to opt in to version 2 of the API</summary>
        /// <example>true</example>
        [FromHeader(Name = "useV2")]
        public bool? UseV2 { get; set; } = null;

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var validationResult = base.Validate(validationContext).ToList();

            if (MatchLevel is not null && !Match)
            {
                validationResult.Add(
                    new ValidationResult(
                        $"{nameof(MatchLevel).ToCamelCase()} must not be set if {nameof(Match).ToCamelCase()} is false",
                        [nameof(MatchLevel).ToCamelCase()]
                    )
                );
            }

            return validationResult;
        }
    }
}
