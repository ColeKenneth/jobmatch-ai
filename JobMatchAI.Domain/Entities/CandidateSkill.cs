using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobMatchAI.Domain.Entities;

[Table("candidate_skills")]
public class CandidateSkill
{
    [Key]
    [Column("id")]
    public Guid Id { get; init; } = Guid.NewGuid();
    
    [Column("candidate_id")]
    public Guid CandidateId { get; init; }
    
    [ForeignKey(nameof(CandidateId))]
    public CandidateResume? CandidateResume { get; init; }

    [Column("skill_name", TypeName = "varchar(100)")]
    public string SkillName { get; set; } = string.Empty;
}