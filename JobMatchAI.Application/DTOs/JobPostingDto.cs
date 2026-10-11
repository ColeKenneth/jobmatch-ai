using JobMatchAI.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace JobMatchAI.Application.DTOs
{
    public record JobPostingDto
    {
        public Guid Id { get; init; }

        [Required(ErrorMessage = "Employer ID is required.")]
        public Guid EmployerId { get; init; }

        [Required(ErrorMessage = "Job title is required.")]
        [StringLength(50, ErrorMessage = "Job title cannot exceed 50 characters.")]
        public string Title { get; init; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        public string Description { get; init; } = string.Empty;

        [Required(ErrorMessage = "Requirements are required.")]
        [StringLength(150, ErrorMessage = "Requirements cannot exceed 150 characters.")]
        public string Requirements { get; init; } = string.Empty;

        [StringLength(150, ErrorMessage = "Preferred skills cannot exceed 150 characters.")]
        public string? PreferredSkills { get; init; }

        [Range(0, 50, ErrorMessage = "Minimum experience must be between 0 and 50 years.")]
        public int MinExperienceYears { get; init; }

        [Range(0, double.MaxValue, ErrorMessage = "Minimum salary cannot be negative.")]
        public decimal? MinSalary { get; init; }

        [Range(0, double.MaxValue, ErrorMessage = "Maximum salary cannot be negative.")]
        public decimal? MaxSalary { get; init; }

        [Required(ErrorMessage = "Location is required.")]
        [StringLength(100, ErrorMessage = "Location cannot exceed 100 characters.")]
        public string Location { get; init; } = string.Empty;

        [EnumDataType(typeof(EmploymentType), ErrorMessage = "Invalid employment type.")]
        public EmploymentType EmploymentType { get; init; }

        public DateTime PostedDate { get; init; }

        public DateTime? Deadline { get; init; }

        public bool IsActive { get; init; }

        public int Views { get; init; }

        public int Applications { get; init; }
    }
}