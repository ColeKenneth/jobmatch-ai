using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace JobMatchAI.Application.DTOs
{
    public record FeatureContribution
    {
        [Required(ErrorMessage = "Feature name is required.")]
        [StringLength(200, ErrorMessage = "Feature name cannot exceed 200 characters.")]
        public string FeatureName { get; init; } = string.Empty;

        [Range(-100.00, 100.00, ErrorMessage = "Value must be between -100 and 100.")]
        public double Value { get; init; }

        [Range(0.0, 1.0, ErrorMessage = "Importance is only between 0.0 and 1.0")]
        public double Importance { get; init; }

        [Required(ErrorMessage = "Impact is required.")]
        public string HumanReadableImpact { get; init; } = string.Empty;
    }
}
