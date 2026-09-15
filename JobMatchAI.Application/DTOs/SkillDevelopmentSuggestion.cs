using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace JobMatchAI.Application.DTOs
{
    public record SkillDevelopmentSuggestion
    {
        [Required(ErrorMessage = "Skill name is required.")]
        [StringLength(maximumLength: 100, ErrorMessage = "Skill name cannot exceed 100 characters.")]
        public string SkillName { get; init; } = string.Empty;

        [Required(ErrorMessage = "Recommendation is required.")]
        [StringLength(maximumLength: 200, ErrorMessage = "Recommendation cannot exceed 200 characters.")]
        public string Recommendation { get; init; } = string.Empty;

        [StringLength(maximumLength: 500, ErrorMessage = "Resource URL cannot exceed 500 characters.")]
        public string? ResourceUrl { get; init; }

        [Range(typeof(int), minimum: "1", maximum: "500", ErrorMessage = "Estimated hours is between 1 and 500 hours only.")]
        public int EstimatedHoursToLearn { get; init; }
    }
}
