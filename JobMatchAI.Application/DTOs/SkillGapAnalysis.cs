using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace JobMatchAI.Application.DTOs
{
    public record SkillGapAnalysis
    {
        public IReadOnlyList<string> MatchedSkills { get; init; } = [];
        public IReadOnlyList<string> MissingRequiredSkills { get; init; } = [];
        public IReadOnlyList<string> MissingPreferrredSkills { get; init; } = [];

        [Range(minimum: 0.00, maximum: 100.00, ErrorMessage = "Match percentage must only between 0 and 100%.")]
        public double MatchPercentage { get; init; }

        public IReadOnlyList<SkillDevelopmentSuggestion> Suggestions { get; init; } = [];
    }
}
