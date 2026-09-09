using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace JobMatchAI.Domain.Entities
{
    [Table("alumni")]
    public class Alumni
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("user_id")]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }

        [Column("program", TypeName = "varchar(30)")]
        public string Program { get; set; } = string.Empty;

        [Column("graduation_year")]
        public DateOnly GraduationYear { get; set; }

        [Column("current_employer", TypeName = "varchar(100)")]
        public string? CurrentEmployer { get; set; }

        [Column("current_position", TypeName = "varchar(50)")]
        public string? CurrentPosition { get; set; }

        // public virtual ICollection<EmploymentHistory> EmploymentHistory { get; set; } = [];
        public virtual ICollection<Skill> Skills { get; set; } = [];
    }
}
