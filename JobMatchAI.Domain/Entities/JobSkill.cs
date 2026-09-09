using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace JobMatchAI.Domain.Entities
{
    [Table("job_skills")]
    public class JobSkill
    {
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("job_posting_id")]
        public Guid JobPostingId { get; set; }

        [ForeignKey(nameof(JobPostingId))]
        public virtual JobPosting? JobPosting { get; set; }

        [Column("skill_id")]
        public Guid SkillId { get; set; }

        [ForeignKey(nameof(SkillId))]
        public virtual Skill? Skill { get; set; }

        [Column("is_required", TypeName = "bool")]
        public bool IsRequired { get; set; } = true;

        [Column("min_proficiency_level", TypeName = "int")]
        public int? MinimumProficiencyLevel { get; set; }

        [Column("priority", TypeName = "int")]
        public int? Priority { get; set; }
    }
}
