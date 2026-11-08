using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobMatchAI.Domain.Entities
{
    [Table("employment_histories")]
    public class EmploymentHistory
    {
        [Key]
        [Column("id")]
        public Guid Id { get; init; } = Guid.NewGuid();

        [Column("alumni_id")]
        public Guid AlumniId { get; init; }

        [ForeignKey(nameof(AlumniId))]
        public Alumni? Alumni { get; init; }

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

        [Column("responsibilities", TypeName = "text")]
        public string? Responsibilities { get; set; }
    }
}
