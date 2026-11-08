using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobMatchAI.Domain.Entities
{
    [Table("alumni")]
    public class Alumni
    {
        [Key]
        [Column("id")]
        public Guid Id { get; init; } = Guid.NewGuid();

        [Column("user_id")]
        public Guid UserId { get; init; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; init; }

        [Column("program", TypeName = "varchar(100)")]
        public string Program { get; set; } = string.Empty;

        [Column("graduation_year", TypeName = "date")]
        public DateOnly GraduationDate { get; set; }

        [Column("current_employer", TypeName = "varchar(100)")]
        public string? CurrentEmployer { get; set; }

        [Column("current_position", TypeName = "varchar(50)")]
        public string? CurrentPosition { get; set; }

        [NotMapped]
        public int GraduationYear => GraduationDate.Year;

        public ICollection<EmploymentHistory> EmploymentHistory { get; init; } = [];
        public ICollection<Skill> Skills { get; init; } = [];
    }
}
