namespace JobMatchAI.Infrastructure.AI.Data;

public record RankedCandidateResult(
    Guid CandidateId,
    double SemanticSimilarity,
    float PredictedMatchScore,
    IReadOnlyDictionary<string, float> FeatureContributions
    );