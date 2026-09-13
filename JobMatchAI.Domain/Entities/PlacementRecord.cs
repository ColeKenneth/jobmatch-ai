using JobMatchAI.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace JobMatchAI.Domain.Entities
{
    [Table("placement_records")]
    public class PlacementRecord
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

        [Column("placement_date", TypeName = "timestamp")]
        public DateTime PlacementDate { get; set; }

        [Column("placement_status", TypeName = "varchar(30)")]
        public PlacementStatus PlacementStatus { get; set; }

        [Column("notes", TypeName = "text")]
        public string? Notes { get; set; }
    }
}
