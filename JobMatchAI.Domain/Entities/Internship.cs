using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace JobMatchAI.Domain.Entities
{
    [Table("internships")]
    public class Internship
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("student_id")]
        public Guid StudentId { get; set; }

        [ForeignKey(nameof(StudentId))]
        public virtual Student? Student { get; set; }

        [Column("company_name", TypeName = "varchar(100)")]
        public string CompanyName { get; set; } = string.Empty;

        [Column("position", TypeName = "varchar(50)")]
        public string Position { get; set; } = string.Empty;

        [Column("start_date", TypeName = "date")]
        public DateOnly StartDate { get; set; }

        [Column("end_date", TypeName = "date")]
        public DateOnly EndDate { get; set; }

        [Column("skills_gained", TypeName = "varchar(200)")]
        public string? SkillsGained { get; set; }

        [Column("supervisor", TypeName = "varchar(150)")]
        public string? Supervisor { get; set; }

        [Column("supervisor_email", TypeName = "varchar(150)")]
        public string? SupervisorEmail { get; set; }
    }
}
