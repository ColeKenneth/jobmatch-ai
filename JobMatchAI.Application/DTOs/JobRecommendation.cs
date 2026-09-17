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
        [StringLength(50, ErrorMessage = "Job title cannot exceed 50 characters.")]
        public string Title { get; init; } = string.Empty;

        [Required(ErrorMessage = "Company name is required.")]
        [StringLength(100, ErrorMessage = "Company name cannot exceed 100 characters.")]
        public string CompanyName { get; init; } = string.Empty;

        [Required(ErrorMessage = "Location is required.")]
        [StringLength(100, ErrorMessage = "Location cannot exceed 100 characters.")]
        public string Location { get; init; } = string.Empty;

        [Range(0.00, 100.00, ErrorMessage = "Match score is only between 0% and 100%.")]
        public double MatchScore { get; init; }

        public SkillGapAnalysis SkillsGap { get; init; } = new();

        public XaiExplanation Explanation { get; init; } = new();

        [PastOrPresent(ErrorMessage = "Recommendation date cannot happen in the future.")]
        public DateTime RecommendationDate { get; init; }
    }
}
