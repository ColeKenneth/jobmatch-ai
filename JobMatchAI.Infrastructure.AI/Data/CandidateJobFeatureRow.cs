namespace JobMatchAI.Infrastructure.AI.Data;

public sealed class CandidateJobFeatureRow
{
    public float SemanticSimilarity { get; set; }
    public float SkillOverlapRatio { get; set; }
    public float WeightedSkillAlignment { get; set; }
    public float GwaNormalized { get; set; }
    public float InternshipCount { get; set; }
    public float CertificationCount { get; set; }
    public float YearsSinceGraduation { get; set; }
}