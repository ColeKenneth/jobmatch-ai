namespace JobMatchAI.Infrastructure.AI.Data;

public sealed record FeatureAttributions(
    string FeatureName,
    float FeatureValue,
    float Contribution
    );