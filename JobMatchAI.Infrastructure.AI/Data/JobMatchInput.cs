using Microsoft.ML.Data;

namespace JobMatchAI.Infrastructure.AI.Data;

public class JobMatchInput
{
    [LoadColumn(0)] 
    public float RequiredSkillRatio { get; set; }
    
    [LoadColumn(1)] 
    public float GwaScore { get; set; }
    
    [LoadColumn(2)]
    public float ProficiencyDepth { get; set; }
    
    [LoadColumn(3)]
    public float CertificationCount { get; set; }
    
    [LoadColumn(4)]
    public float InternshipCount { get; set; }
    
    [LoadColumn(5)]
    public float Label { get; set; }
}