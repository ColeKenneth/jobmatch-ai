using JobMatchAI.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobMatchAI.Domain.Entities
{
    [Table("job_postings")]
    public class JobPosting
    {
        [Key]
        [Column("id")]
        public Guid Id { get; init; } = Guid.NewGuid();

        [Column("employer_id")]
        public Guid EmployerId { get; init; }

        [ForeignKey(nameof(EmployerId))]
        public Employer? Employer { get; init; }

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

        [Column("min_salary", TypeName = "decimal(9,2)")]
        public decimal? MinSalary { get; set; }

        [Column("max_salary", TypeName = "decimal(9,2)")]
        public decimal? MaxSalary { get; set; }

        [Column("location", TypeName = "varchar(100)")]
        public string Location { get; set; } = string.Empty;

        [Column("employment_type", TypeName = "varchar(20)")]
        public EmploymentType EmploymentType { get; set; }

        [Column("posted_date", TypeName = "timestamp")]
        public DateTime PostedDate { get; init; } = DateTime.UtcNow;

        [Column("deadline", TypeName = "timestamp")]
        public DateTime? Deadline { get; set; }

        [Column("is_active", TypeName = "bool")]
        public bool IsActive { get; set; } = true;

        [Column("views", TypeName = "int")]
        public int Views { get; init; }

        [Column("applications", TypeName = "int")]
        public int Applications { get; init; }

        public ICollection<JobSkill> RequiredSkills { get; init; } = [];
        public ICollection<MatchResult> MatchResults { get; init; } = [];
        public ICollection<PlacementRecord> PlacementRecords { get; init; } = [];
    }
}
