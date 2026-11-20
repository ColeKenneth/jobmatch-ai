namespace JobMatchAI.Infrastructure.AI.Services;

public interface ICosineSimilarityService
{
    double CalcuateSimilarity(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB);
}