namespace JobMatchAI.Infrastructure.AI.Data;

public record CandidateRankingExplanation(
    CandidatePredictionOutput Prediction,
    IReadOnlyList<FeatureAttributions> Attributions
    );