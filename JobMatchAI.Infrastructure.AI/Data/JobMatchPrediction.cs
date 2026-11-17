using Microsoft.ML.Data;

namespace JobMatchAI.Infrastructure.AI.Data;

public class JobMatchPrediction
{
    [ColumnName("Score")]
    public float Score { get; set; }

    [ColumnName("FeatureContributions")]
    public float[] FeatureContributions { get; set; } = [];
}