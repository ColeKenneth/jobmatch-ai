namespace JobMatchAI.Infrastructure.AI.Services;

public sealed class CosineSimilarityService : ICosineSimilarityService
{
    public double CalcuateSimilarity(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    {
        if (vectorA.IsEmpty || vectorB.IsEmpty || vectorA.Length != vectorB.Length)
        {
            return 0.0;
        }

        var dotProduct = 0.0;
        var normA = 0.0;
        var normB = 0.0;

        for (var i = 0; i < vectorA.Length; i++)
        {
            double a = vectorA[i];
            double b = vectorB[i];
            
            dotProduct += a * b;
            normA += a * a;
            normB += b * b;
        }

        if (normA <= 1e-12 || normB <= 1e-12)
        {
            return 0.0;
        }

        var similarity = dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
        return Math.Clamp(similarity, -1.0, 1.0);
    }
}