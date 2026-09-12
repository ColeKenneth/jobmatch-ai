using JobMatchAI.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobMatchAI.Domain.Entities
{
    [Table("students")]
    public class Student
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("user_id")]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }

        [Column("student_id_number", TypeName = "varchar(30)")]
        public string StudentIdNumber { get; set; } = string.Empty;

        [Column("program", TypeName = "varchar(100)")]
        public string Program { get; set; } = string.Empty;

        [Column("year_level", TypeName = "varchar(20)")]
        public YearLevel YearLevel { get; set; }

        [Column("gwa", TypeName = "decimal(3,2)")]
        public decimal Gwa { get; set; }

        [Column("graduation_date", TypeName = "date")]
        public DateOnly? GraduationDate { get; set; }

        [Column("resume_file_path", TypeName = "varchar(100)")]
        public string? ResumeFilePath { get; set; }

        [Column("resume_text", TypeName = "text")]
        public string? ResumeText { get; set; }

        public virtual ICollection<Skill> Skills { get; set; } = [];
        public virtual ICollection<MatchResult> MatchResults { get; set; } = [];
    }
}