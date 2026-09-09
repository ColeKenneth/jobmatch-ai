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
    }
}
