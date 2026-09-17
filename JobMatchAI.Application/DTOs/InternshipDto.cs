using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace JobMatchAI.Application.DTOs
{
    public record InternshipDto
    {
        [Required(ErrorMessage = "Company name is required.")]
        [StringLength(100, ErrorMessage = "Company name cannot exceed 100 characters.")]
        public string CompanyName { get; init; } = string.Empty;

        [Required(ErrorMessage = "Position is required.")]
        [StringLength(50, ErrorMessage = "Position name cannot exceed 50 characters.")]
        public string Position { get; init; } = string.Empty;

        [Range(1, 24, ErrorMessage = "Duration must be between 1 and 24 months.")]
        public int DurationMonths { get; init; }

        [StringLength(200, ErrorMessage = "Skills gained cannot exceed 200 characters.")]
        public string? SkillsGained { get; init; }
    }
}
