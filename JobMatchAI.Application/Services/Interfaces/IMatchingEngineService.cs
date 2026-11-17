using JobMatchAI.Application.DTOs;

namespace JobMatchAI.Application.Services.Interfaces;

public interface IMatchingEngineService
{
    Task<XaiExplanation> EvaluateMatchAsync(Guid studentId, Guid jobPostingId, CancellationToken ct = default);
}