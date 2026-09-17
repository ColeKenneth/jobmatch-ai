using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace JobMatchAI.Application.DTOs
{
    public record EmployabilityPrediction
    {
        public Guid StudentId { get; init; }

        [Range(0.00, 100.00, ErrorMessage = "Employability score is between 0% and 100%.")]
        public double EmployabilityScore { get; init; }

        [Required(ErrorMessage = "Employability level is required.")]
        [StringLength(100, ErrorMessage = "Employability level cannot exceed 100 characters.")]
        public string EmployabilityLevel { get; init; } = string.Empty;

        public IReadOnlyList<string> Strengths { get; init; } = [];

        public IReadOnlyList<string> AreasForImprovement { get; init; } = [];

        public IReadOnlyList<string> RecommendedCertifications { get; init; } = [];

        public IReadOnlyList<string> RecommendedCourses { get; init; } = [];

        public IReadOnlyDictionary<string, double> FeatureImportance { get; init; } = new Dictionary<string, double>();
    }
}
