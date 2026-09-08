using JobMatchAI.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

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

        [Column("student_id", TypeName = "varchar(40)")]
        public string StudentId { get; set; } = string.Empty;

        [Column("program", TypeName = "varchar(50)")]
        public string Program { get; set; } = string.Empty;

        [Column("year_level", TypeName = "varchar(15)")]
        public YearLevel YearLevel { get; set; }

        [Column("gwa", TypeName = "decimal(2,2)")]
        public decimal Gwa { get; set; }

        [Column("graduation_date", TypeName = "date")]
        public DateOnly? GraduationDate { get; set; }

        [Column("resume_file_path", TypeName = "varchar(10)")]
        public string? ResumeFilePath { get; set; }

        [Column("resume_text", TypeName = "text")]
        public string? ResumeText { get; set; }
    }
}
