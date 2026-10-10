using System.ComponentModel.DataAnnotations;

namespace JobMatchAI.Application.DTOs
{
    public record EmploymentHistoryDto
    {
        public Guid Id { get; init; }

        public Guid AlumniId { get; init; }

        [Required(ErrorMessage = "Company name is required.")]
        [StringLength(100, ErrorMessage = "Company name cannot exceed 100 characters.")]
        public string CompanyName { get; init; } = string.Empty;

        [Required(ErrorMessage = "Position is required.")]
        [StringLength(100, ErrorMessage = "Position cannot exceed 100 characters.")]
        public string Position { get; init; } = string.Empty;

        [Required(ErrorMessage = "Start date is required.")]
        public DateOnly StartDate { get; init; }

        public DateOnly? EndDate { get; init; }

        public bool IsCurrent { get; init; }

        public string? Responsibilities { get; init; }
    }
}