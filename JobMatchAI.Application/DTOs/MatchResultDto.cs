using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace JobMatchAI.Application.DTOs
{
    public record MatchResultDto
    {
        public Guid StudentId { get; init; }

        public Guid JobPostingId { get; init; }

        [Required(ErrorMessage = "Job title is required.")]
        [StringLength(100, ErrorMessage = "Job title cannot exceed 100 characters.")]
        public string JobTitle { get; init; } = string.Empty;

        [Required(ErrorMessage = "Company name is required.")]
        [StringLength(100, ErrorMessage = "Company name cannot exceed 100 characters.")]
        public string CompanyName { get; init; } = string.Empty;

        [Range(0.00, 100.00, ErrorMessage = "Matching score is between 0% to 100%.")]
        public double MatchScore { get; init; }

        public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;

        public bool? WasHired { get; init; }

        [StringLength(2000, ErrorMessage = "Match explanation must be 2000 characters maximum.")]
        public string? MatchExplanation { get; init; }
    }
}
