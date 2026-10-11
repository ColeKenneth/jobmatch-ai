using System.ComponentModel.DataAnnotations;

namespace JobMatchAI.Application.DTOs
{
    public record MatchResultDto
    {
        public Guid Id { get; init; }

        public Guid StudentId { get; init; }

        public Guid JobPostingId { get; init; }

        [Range(0.00, 100.00, ErrorMessage = "Matching score must be between 0% and 100%.")]
        public double MatchScore { get; init; }

        [Range(0.00, 100.00, ErrorMessage = "Employability score must be between 0% and 100%.")]
        public double? EmployabilityScore { get; init; }

        public string? MatchExplanation { get; init; }

        public string? FeatureContributions { get; init; }

        public DateTime GeneratedAt { get; init; }

        public bool? WasViewed { get; init; }

        public bool? WasApplied { get; init; }

        public bool? WasHired { get; init; }

        public DateTime? HireDate { get; init; }
    }
}