using Microsoft.ML.Data;

namespace JobMatchAI.Infrastructure.AI.Data;

public class CandidateFeatureInput
{
    public float Gwa { get; set; }
    public float GraduationYear { get; set; }
    public float SkillMatchScore { get; set; }
    public float CertificationCount { get; set; }
    public float InternshipCount { get; set; }
    public float DegreeRelevanceScore { get; set; }
    
    [ColumnName("Label")]
    public bool IsRecommended { get; set; }
}