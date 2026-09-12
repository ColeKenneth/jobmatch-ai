using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobMatchAI.Domain.Entities
{
    [Table("match_results")]
    public class MatchResult
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("student_id")]
        public Guid StudentId { get; set; }

        [ForeignKey(nameof(StudentId))]
        public virtual Student? Student { get; set; }

        [Column("job_posting_id")]
        public Guid JobPostingId { get; set; }

        [ForeignKey(nameof(JobPostingId))]
        public virtual JobPosting? JobPosting { get; set; }

        [Column("match_score", TypeName = "decimal(5,2)")]
        public double MatchScore { get; set; }

        [Column("employability_score", TypeName = "decimal(5,2)")]
        public double? EmployabilityScore { get; set; }

        [Column("match_explanation", TypeName = "text")]
        public string? MatchExplanation { get; set; }

        [Column("feature_contributions", TypeName = "text")]
        public string? FeatureContributions { get; set; }

        [Column("generated_at", TypeName = "timestamp")]
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        [Column("was_viewed", TypeName = "boolean")]
        public bool? WasViewed { get; set; }

        [Column("was_applied", TypeName = "boolean")]
        public bool? WasApplied { get; set; }

        [Column("was_hired", TypeName = "boolean")]
        public bool? WasHired { get; set; }

        [Column("hire_date", TypeName = "timestamp")]
        public DateTime? HireDate { get; set; }
    }

}