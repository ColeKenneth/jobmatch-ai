namespace JobMatchAI.Infrastructure.AI.Services;

public interface IEmbeddingService
{
    float[] GenerateEmbeddings(string text);
    Task<float[]> GenerateEmbeddingsAsync(string text, CancellationToken ct = default);
}