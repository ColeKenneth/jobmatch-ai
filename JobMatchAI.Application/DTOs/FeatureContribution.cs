using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace JobMatchAI.Application.DTOs
{
    public record FeatureContribution
    {
        [Required(ErrorMessage = "Feature name is required.")]
        [StringLength(maximumLength: 200, ErrorMessage = "Feature name cannot exceed 200 characters.")]
        public string FeatureName { get; init; } = string.Empty;

        [Range(minimum: 1, maximum: 100, ErrorMessage = "Value is only between 1 and 100.")]
        public double Value { get; init; }

        [Range(minimum: 1, maximum: 5, ErrorMessage = "Importance must be rated from 1 to 5 only.")]
        public double Importance { get; init; }

        [Required(ErrorMessage = "Impact is required.")]
        public string HumanReadableImpact { get; init; } = string.Empty;
    }
}
