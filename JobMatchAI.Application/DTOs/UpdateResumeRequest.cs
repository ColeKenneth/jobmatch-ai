using System.ComponentModel.DataAnnotations;

namespace JobMatchAI.Application.DTOs
{
    public record UpdateResumeRequest
    {
        [Required(ErrorMessage = "File path is required.")]
        [StringLength(100, ErrorMessage = "File path cannot exceed 100 characters.")]
        public string FilePath { get; init; } = string.Empty;

        [Required(ErrorMessage = "Resume text is required.")]
        public string ResumeText { get; init; } = string.Empty;
    }
}