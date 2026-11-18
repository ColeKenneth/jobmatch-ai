using JobMatchAI.Infrastructure.AI.Data;

namespace JobMatchAI.Infrastructure.AI.Services;

public interface ICandidateRankingService
{
    CandidatePredictionOutput EvaluateCandidate(CandidateFeatureInput input);
}