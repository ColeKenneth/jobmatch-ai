using JobMatchAI.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace JobMatchAI.Domain.Entities
{
    [Table("job_postings")]
    public class JobPosting
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("employer_id")]
        public Guid EmployerId { get; set; }

        [ForeignKey(nameof(EmployerId))]
        public virtual Employer? Employer { get; set; }

        [Column("title", TypeName = "varchar(50)")]
        public string Title { get; set; } = string.Empty;

        [Column("description", TypeName = "text")]
        public string Description { get; set; } = string.Empty;

        [Column("requirements", TypeName = "varchar(150)")]
        public string Requirements { get; set; } = string.Empty;

        [Column("preferred_skills", TypeName = "varchar(150)")]
        public string? PreferredSkills { get; set; }

        [Column("min_experience_years", TypeName = "int")]
        public int MinExperienceYears { get; set; }

        [Column("min_salary", TypeName = "decimal(5,2)")]
        public decimal? MinSalary { get; set; }

        [Column("max_salary", TypeName = "decimal(5,2)")]
        public decimal? MaxSalary { get; set; }

        [Column("location", TypeName = "varchar(100)")]
        public string Location { get; set; } = string.Empty;

        [Column("employment_type", TypeName = "varchar(20)")]
        public EmploymentType EmploymentType { get; set; }

        [Column("posted_date", TypeName = "timestamp")]
        public DateTime PostedDate { get; set; } = DateTime.UtcNow;

        [Column("deadline", TypeName = "timestamp")]
        public DateTime? Deadline { get; set; }

        [Column("is_active", TypeName = "bool")]
        public bool IsActive { get; set; } = true;

        [Column("views", TypeName = "int")]
        public int Views { get; set; } = 0;

        [Column("applications", TypeName = "int")]
        public int Applications { get; set; } = 0;

        public virtual ICollection<JobSkill> RequiredSkills { get; set; } = [];
        public virtual ICollection<MatchResult> MatchResults { get; set; } = [];
    }
}
