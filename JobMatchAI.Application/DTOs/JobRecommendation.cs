using JobMatchAI.Application.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace JobMatchAI.Application.DTOs
{
    public record JobRecommendation
    {
        public Guid JobId { get; init; }

        [Required(ErrorMessage = "Job title is required.")]
        [StringLength(maximumLength: 50, ErrorMessage = "Job title cannot exceed 50 characters.")]
        public string Title { get; init; } = string.Empty;

        [Required(ErrorMessage = "Company name is required.")]
        [StringLength(maximumLength: 100, ErrorMessage = "Company name cannot exceed 100 characters.")]
        public string CompanyName { get; init; } = string.Empty;

        [Required(ErrorMessage = "Location is required.")]
        [StringLength(maximumLength: 100, ErrorMessage = "Location cannot exceed 100 characters.")]
        public string Location { get; init; } = string.Empty;

        [Range(minimum: 0.00, maximum: 100.00, ErrorMessage = "Match score is only between 0% and 100%.")]
        public double MatchScore { get; init; }

        public required SkillGapAnalysis SkillsGap { get; init; }

        public required XaiExplanation Explanation { get; init; }

        [FutureDate(ErrorMessage = "Recommendation date cannot happen in the past.")]
        public DateTime RecommendationDate { get; init; }
    }
}
