using System.ComponentModel.DataAnnotations;

namespace JobMatchAI.Application.DTOs
{
    public record AlumniDto
    {
        public Guid Id { get; init; }

        public Guid UserId { get; init; }

        [Required(ErrorMessage = "Program name is required.")]
        [StringLength(100, ErrorMessage = "Program name cannot exceed 100 characters.")]
        public string Program { get; init; } = string.Empty;

        public DateOnly GraduationDate { get; init; }

        [StringLength(100, ErrorMessage = "Current employer cannot exceed 100 characters.")]
        public string? CurrentEmployer { get; init; }

        [StringLength(50, ErrorMessage = "Current position cannot exceed 50 characters.")]
        public string? CurrentPosition { get; init; }

        public int GraduationYear => GraduationDate.Year;
    }
}
