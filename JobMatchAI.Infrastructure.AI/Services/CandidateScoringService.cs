using JobMatchAI.Application.DTOs;
using JobMatchAI.Infrastructure.AI.Data;

namespace JobMatchAI.Infrastructure.AI.Services;

public class CandidateScoringService(ICandidateRankingService rankingService) : ICandidateScoringService
{
    public Task<CandidatePredictionOutput> ScoreCandidateAsync(ExtractedResumeDataDto resumeDto, JobRequirementDto jobDto, CancellationToken ct = default)
    {
        var skillMatchScore = CalculateSkillOverlap(resumeDto.ExtractedSkills, jobDto.RequiredSkills);
        var degreeRelevanceScore = EvaluateDegreeRelevance(resumeDto.DegreeProgram, jobDto.TargetDegreePrograms);

        var featureInput = new CandidateFeatureInput
        {
            Gwa = (float)resumeDto.Gwa,
            GraduationYear = resumeDto.GraduationYear,
            SkillMatchScore = skillMatchScore,
            CertificationCount = resumeDto.CertificationCount,
            DegreeRelevanceScore = degreeRelevanceScore,
            InternshipCount = resumeDto.InternshipCount
        };

        var prediction = rankingService.EvaluateCandidate(featureInput);
        
        return Task.FromResult(prediction);
    }

    private static float CalculateSkillOverlap(IReadOnlyList<ExtractedSkillDto> candidateSkills,
        IReadOnlyList<string> requiredSkills)
    {
        if (requiredSkills.Count == 0)
        {
            return 1.0f;
        }

        var candidateSkillNames = candidateSkills.Select(s => s.SkillName.ToLowerInvariant()).ToHashSet();
        var matchedCount = requiredSkills.Count(req => candidateSkillNames.Contains(req.ToLowerInvariant()));

        return (float)matchedCount / requiredSkills.Count;
    }

    private static float EvaluateDegreeRelevance(string candidateDegree, IReadOnlyList<string> targetDegrees)
    {
        if (targetDegrees.Count == 0)
        {
            return 1.0f;
        }

        return targetDegrees.Any(d => string.Equals(d, candidateDegree, StringComparison.OrdinalIgnoreCase)) ? 1.0f : 0.5f;
    }
}