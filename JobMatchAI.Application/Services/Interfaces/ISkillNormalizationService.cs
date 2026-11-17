namespace JobMatchAI.Application.Services.Interfaces;

public interface ISkillNormalizationService
{
    Task NormalizeAndLinkSkillAsync(Guid studentId, IEnumerable<string> rawSkillNames, CancellationToken ct = default);
}