using JobMatchAI.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobMatchAI.Domain.Entities
{
    [Table("placement_records")]
    public class PlacementRecord
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

        [Column("placement_date", TypeName = "timestamp")]
        public DateTime PlacementDate { get; set; }

        [Column("placement_status", TypeName = "varchar(30)")]
        public PlacementStatus PlacementStatus { get; set; }

        [Column("notes", TypeName = "text")]
        public string? Notes { get; set; }
    }
}
