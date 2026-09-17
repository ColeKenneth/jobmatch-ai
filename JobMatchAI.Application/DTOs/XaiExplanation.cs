using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace JobMatchAI.Application.DTOs
{
    public record XaiExplanation
    {
        [Range(0.00, 100.00, ErrorMessage = "Matching score is between 0% to 100%.")]
        public double MatchScore { get; init; }

        public IReadOnlyList<FeatureContribution> FeatureContributions { get; init; } = [];

        public string Summary { get; init; } = string.Empty;

        public IReadOnlyList<string> Strengths { get; init; } = [];

        public IReadOnlyList<string> Weaknesses { get; init; } = [];

        public IReadOnlyList<string> Recommendations { get; init; } = [];

        public string ReadableScore => $"{MatchScore:F1}%";
        public bool IsStrongMatch => MatchScore >= 70.00;
    }
}
