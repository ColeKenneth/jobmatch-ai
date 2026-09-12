using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace JobMatchAI.Domain.Entities
{
    [Table("certifications")]
    public class Certification
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("student_id")]
        public Guid StudentId { get; set; }

        [ForeignKey(nameof(StudentId))]
        public virtual Student? Student { get; set; }

        [Column("name", TypeName = "varchar(100)")]
        public string Name { get; set; } = string.Empty;

        [Column("issuing_authority", TypeName = "varchar(100)")]
        public string IssuingAuthority { get; set; } = string.Empty;

        [Column("date_issued", TypeName = "timestamp")]
        public DateTime DateIssued { get; set; }

        [Column("expiry_date", TypeName = "timestamp")]
        public DateTime? ExpiryDate { get; set; }

        [Column("verification_url", TypeName = "varchar(100)")]
        public string? VerificationUrl { get; set; }

        [Column("credential_id", TypeName = "varchar(50)")]
        public string? CredentialId { get; set; }
    }
}
