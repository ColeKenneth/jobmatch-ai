using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobMatchAI.Domain.Entities
{
    [Table("student_skills")]
    public class StudentSkill
    {
        [Key]
        [Column("id")]
        public Guid Id { get; init; } = Guid.NewGuid();

        [Column("student_id")]
        public Guid StudentId { get; init; }

        [ForeignKey(nameof(StudentId))]
        public Student? Student { get; init; }

        [Column("skill_id")]
        public Guid SkillId { get; init; }

        [ForeignKey(nameof(SkillId))]
        public Skill? Skill { get; init; } 

        [Column("proficiency_level", TypeName = "int")]
        public int ProficiencyLevel { get; set; } = 1;

        [Column("years_of_experience", TypeName = "int")]
        public int? YearsOfExperience { get; set; }
    }
}
