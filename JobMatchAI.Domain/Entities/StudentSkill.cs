using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace JobMatchAI.Domain.Entities
{
    [Table("student_skills")]
    public class StudentSkill
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("student_id")]
        public Guid StudentId { get; set; }

        [ForeignKey(nameof(StudentId))]
        public virtual Student? Student { get; set; }

        [Column("skill_id")]
        public Guid SkillId { get; set; }

        [ForeignKey(nameof(SkillId))]
        public virtual Skill? Skill { get; set; } 

        [Column("proficiency_level", TypeName = "int")]
        public int ProficiencyLevel { get; set; } = 1;

        [Column("years_of_experience", TypeName = "int")]
        public int? YearsOfExperience { get; set; }
    }
}
