using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace JobMatchAI.Domain.Entities
{
    [Table("employment_histories")]
    public class EmploymentHistory
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("alumni_id")]
        public Guid AlumniId { get; set; }

        [ForeignKey(nameof(AlumniId))]
        public virtual Alumni? Alumni { get; set; }

        [Column("company_name", TypeName = "varchar(100)")]
        public string CompanyName { get; set; } = string.Empty;

        [Column("position", TypeName = "varchar(100)")]
        public string Position { get; set; } = string.Empty;

        [Column("start_date", TypeName = "date")]
        public DateOnly StartDate { get; set; }

        [Column("end_date", TypeName = "date")]
        public DateOnly? EndDate { get; set; }

        [Column("is_current", TypeName = "boolean")]
        public bool IsCurrent { get; set; }

        [Column("responsibilities", TypeName = "varchar(200)")]
        public string? Responsibilities { get; set; }
    }
}
