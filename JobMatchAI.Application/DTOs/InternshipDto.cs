using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace JobMatchAI.Application.DTOs
{
    public record InternshipDto
    {
        public Guid Id { get; init; }

        public Guid StudentId { get; init; }

        [Required(ErrorMessage = "Company name is required.")]
        [StringLength(100, ErrorMessage = "Company name cannot exceed 100 characters.")]
        public string CompanyName { get; init; } = string.Empty;

        [Required(ErrorMessage = "Position is required.")]
        [StringLength(50, ErrorMessage = "Position name cannot exceed 50 characters.")]
        public string Position { get; init; } = string.Empty;

        [Required(ErrorMessage = "Start date is required.")]
        public DateOnly StartDate { get; init; }

        [Required(ErrorMessage = "End date is required.")]
        public DateOnly EndDate { get; init; }

        [StringLength(200, ErrorMessage = "Skills gained cannot exceed 200 characters.")]
        public string? SkillsGained { get; init; }

        [StringLength(150, ErrorMessage = "Supervisor name cannot exceed 150 characters.")]
        public string? Supervisor { get; init; }

        [EmailAddress(ErrorMessage = "Invalid supervisor email format.")]
        [StringLength(150, ErrorMessage = "Supervisor email cannot exceed 150 characters.")]
        public string? SupervisorEmail { get; init; }

        [NotMapped]
        public int DurationMonths => ((EndDate.Year - StartDate.Year) * 12) + (EndDate.Month - StartDate.Month);
    }
}
