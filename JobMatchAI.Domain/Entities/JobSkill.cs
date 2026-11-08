using System.ComponentModel.DataAnnotations.Schema;

namespace JobMatchAI.Domain.Entities
{
    [Table("job_skills")]
    public class JobSkill
    {
        [Column("id")]
        public Guid Id { get; init; } = Guid.NewGuid();

        [Column("job_posting_id")]
        public Guid JobPostingId { get; init; }

        [ForeignKey(nameof(JobPostingId))]
        public JobPosting? JobPosting { get; init; }

        [Column("skill_id")]
        public Guid SkillId { get; init; }

        [ForeignKey(nameof(SkillId))]
        public Skill? Skill { get; init; }

        [Column("is_required", TypeName = "bool")]
        public bool IsRequired { get; set; } = true;

        [Column("min_proficiency_level", TypeName = "int")]
        public int? MinimumProficiencyLevel { get; set; }

        [Column("priority", TypeName = "int")]
        public int? Priority { get; set; }
    }
}
