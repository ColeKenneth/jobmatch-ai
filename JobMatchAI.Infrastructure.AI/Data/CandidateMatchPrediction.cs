using Microsoft.ML.Data;

namespace JobMatchAI.Infrastructure.AI.Data;

public sealed class CandidateMatchPrediction
{
    [ColumnName("Score")]
    public float PredictedScore { get; set; }
}