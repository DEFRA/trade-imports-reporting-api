using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Defra.TradeImportsReportingApi.Api.Endpoints.Dtos
{
    public class DateRangeRequest : IValidatableObject
    {
        /// <summary>ISO 8609 UTC only</summary>
        /// <example>2025-09-10T11:08:48Z</example>
        [Required]
        [FromQuery(Name = "from")]
        public DateTime From { get; set; }

        /// <summary>ISO 8609 UTC only</summary>
        /// <example>2025-09-10T11:08:48Z</example>
        [Required]
        [FromQuery(Name = "to")]
        public DateTime To { get; set; }

        public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var validationResults = new List<ValidationResult>();

            if (From > To)
            {
                validationResults.Add(
                    new ValidationResult(
                        $"{nameof(From).ToCamelCase()} cannot be greater than {nameof(To).ToCamelCase()}",
                        [nameof(From).ToCamelCase()]
                    )
                );
            }

            if (To.Subtract(From).Days > TimePeriod.MaxDays)
            {
                validationResults.Add(
                    new ValidationResult($"date span cannot be greater than {TimePeriod.MaxDays} days", [])
                );
            }

            if (From.Kind != DateTimeKind.Utc)
            {
                validationResults.Add(new ValidationResult("date must be UTC", [nameof(From).ToCamelCase()]));
            }

            if (To.Kind != DateTimeKind.Utc)
            {
                validationResults.Add(new ValidationResult("date must be UTC", [nameof(To).ToCamelCase()]));
            }

            return validationResults;
        }
    }
}
