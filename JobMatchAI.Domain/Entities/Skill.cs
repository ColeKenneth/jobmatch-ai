using JobMatchAI.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace JobMatchAI.Domain.Entities
{
    [Table("skills")]
    public class Skill
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("name", TypeName = "varchar(100)")]
        public string Name { get; set; } = string.Empty;

        [Column("category", TypeName = "varchar(20)")]
        public Category Category { get; set; }

        [Column("ontology_id")]
        public string? OntologyId { get; set; }

        [Column("description", TypeName = "text")]
        public string? Description { get; set; }

        [Column("icon_url", TypeName = "varchar(500)")]
        public string? IconUrl { get; set; }

        public virtual ICollection<StudentSkill> StudentSkills { get; set; } = [];
        public virtual ICollection<JobSkill> JobSkills { get; set; } = [];
    }
}
