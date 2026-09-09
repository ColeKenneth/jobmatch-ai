using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace JobMatchAI.Domain.Entities
{
    [Table("employers")]
    public class Employer
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("user_id")]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }

        [Column("company_name", TypeName = "varchar(80)")]
        public string CompanyName { get; set; } = string.Empty;

        [Column("industry", TypeName = "varchar(80)")]
        public string Industry { get; set; } = string.Empty;

        [Column("website", TypeName = "varchar(50)")]
        public string? Website { get; set; }

        [Column("company_description", TypeName = "text")]
        public string? CompanyDescription { get; set; }

        [Column("logo_url", TypeName = "varchar(50)")]
        public string? LogoUrl { get; set; }

        public virtual ICollection<JobPosting> JobPostings { get; set; } = [];


    }
}
