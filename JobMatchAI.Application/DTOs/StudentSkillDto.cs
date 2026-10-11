using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace JobMatchAI.Application.DTOs
{
    public record StudentSkillDto
    {
        public Guid Id { get; init; }

        public Guid StudentId { get; init; }

        [Required(ErrorMessage = "Skill ID is required.")]
        public Guid SkillId { get; init; }

        [Range(1, 5, ErrorMessage = "Proficiency level must be between 1 and 5.")]
        public int ProficiencyLevel { get; init; }

        [Range(0, 50, ErrorMessage = "Years of experience must be between 0 and 50.")]
        public int? YearsOfExperience { get; init; }
    }
}
