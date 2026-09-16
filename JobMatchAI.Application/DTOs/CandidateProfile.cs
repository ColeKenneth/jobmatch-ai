using JobMatchAI.Application.Attributes;
using JobMatchAI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace JobMatchAI.Application.DTOs
{
    public record CandidateProfile
    {
        public Guid Id { get; init; }

        [Required(ErrorMessage = "Your full name is required.")]
        [StringLength(maximumLength: 150, ErrorMessage = "Full name cannot exceed 150 characters.")]
        public string FullName { get; init; } = string.Empty;

        [Required(ErrorMessage = "Your program name is required.")]
        [StringLength(maximumLength: 100, ErrorMessage = "Program name cannot exceed 100 characters.")]
        public string Program { get; init; } = string.Empty;

        [NonNegativeNumber]
        [Range(typeof(decimal), "1.00", "5.00", ErrorMessage = "GWA must be between 1.00 and 5.00")]
        public decimal Gwa { get; init; }

        public IReadOnlyList<SkillDto> Skills { get; init; } = [];
        public IReadOnlyList<CertificationDto> Certifications { get; init; } = [];
        public IReadOnlyList<InternshipDto> Internships { get; init; } = [];

        [Range(minimum: 0, maximum: 500, ErrorMessage = "Total experience must be between 0 and 500 months.")]
        public int TotalExperienceMonths { get; init; }

        public IReadOnlyDictionary<string, double> FeatureVector { get; init; } = new Dictionary<string, double>();
    }
}
