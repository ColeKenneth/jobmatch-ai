namespace JobMatchAI.Infrastructure.AI.Services;

public interface IAnonymizationService
{
    string? ScrubPii(string? rawText);
}