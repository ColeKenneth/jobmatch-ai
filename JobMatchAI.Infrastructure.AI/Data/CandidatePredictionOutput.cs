using Microsoft.ML.Data;

namespace JobMatchAI.Infrastructure.AI.Data;

public class CandidatePredictionOutput
{
    [ColumnName("PredictedLabel")]
    public bool PredictedLabel { get; set; }
    
    [ColumnName("Score")]
    public float Score { get; set; }
    
    [ColumnName("Probability")]
    public float Probability { get; set; }
}