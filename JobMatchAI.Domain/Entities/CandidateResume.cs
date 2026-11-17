using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobMatchAI.Domain.Entities;

[Table("candidate_resumes")]
public class CandidateResume
{
    [Key]
    [Column("id")]
    public Guid Id { get; init; } = Guid.NewGuid();

    [Column("file_name", TypeName = "varchar(255)")]
    public string FileName { get; set; } = string.Empty;

    [Column("raw_text",  TypeName = "text")]
    public string RawText { get; set; } = string.Empty;

    [Column("uploaded_at",  TypeName = "timestamp")]
    public DateTime UploadedAt { get; init; } = DateTime.UtcNow;

    public ICollection<CandidateSkill> Skills { get; set; } = [];
}