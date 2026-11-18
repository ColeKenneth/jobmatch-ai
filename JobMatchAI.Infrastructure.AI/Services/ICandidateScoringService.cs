using JobMatchAI.Application.DTOs;
using JobMatchAI.Infrastructure.AI.Data;

namespace JobMatchAI.Infrastructure.AI.Services;

public interface ICandidateScoringService
{
    Task<CandidatePredictionOutput> ScoreCandidateAsync(ExtractedResumeDataDto resumeDto, JobRequirementDto jobDto,
        CancellationToken ct = default);
}