using System.ComponentModel.DataAnnotations;

namespace JobMatchAI.Application.DTOs
{
    public record JobSkillDto
    {
        public Guid Id { get; init; }

        public Guid JobPostingId { get; init; }

        [Required(ErrorMessage = "Skill ID is required.")]
        public Guid SkillId { get; init; }

        public bool IsRequired { get; init; }

        [Range(1, 5, ErrorMessage = "Proficiency level must be between 1 and 5.")]
        public int? MinimumProficiencyLevel { get; init; }

        [Range(1, 5, ErrorMessage = "Priority must be between 1 and 5.")]
        public int? Priority { get; init; }
    }
}