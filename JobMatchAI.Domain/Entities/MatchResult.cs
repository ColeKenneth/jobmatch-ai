using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobMatchAI.Domain.Entities
{
    [Table("match_results")]
    public class MatchResult
    {
        [Key]
        [Column("id")]
        public Guid Id { get; init; } = Guid.NewGuid();

        [Column("student_id")]
        public Guid StudentId { get; init; }

        [ForeignKey(nameof(StudentId))]
        public Student? Student { get; init; }

        [Column("job_posting_id")]
        public Guid JobPostingId { get; init; }

        [ForeignKey(nameof(JobPostingId))]
        public JobPosting? JobPosting { get; init; }

        [Column("match_score", TypeName = "decimal(5,2)")]
        public double MatchScore { get; set; }

        [Column("employability_score", TypeName = "decimal(5,2)")]
        public double? EmployabilityScore { get; set; }

        [Column("match_explanation", TypeName = "text")]
        public string? MatchExplanation { get; set; }

        [Column("feature_contributions", TypeName = "text")]
        public string? FeatureContributions { get; set; }

        [Column("generated_at", TypeName = "timestamp")]
        public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;

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