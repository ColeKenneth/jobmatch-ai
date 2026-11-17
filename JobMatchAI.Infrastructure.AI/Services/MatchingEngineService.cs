using JobMatchAI.Application.DTOs;
using JobMatchAI.Application.Services.Interfaces;
using JobMatchAI.Domain.Exceptions;
using JobMatchAI.Infrastructure.AI.Data;
using JobMatchAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;

namespace JobMatchAI.Infrastructure.AI.Services;

public class MatchingEngineService(
    ITransformer? model,
    AppDbContext context,
    MLContext mlContext) : IMatchingEngineService
{
    private readonly ITransformer _model = model ?? InitializeOrTrainDefaultModel(mlContext);
    
    public async Task<XaiExplanation> EvaluateMatchAsync(Guid studentId, Guid jobPostingId, CancellationToken ct = default)
    {
        var student = await context.Students
                          .Include(s => s.StudentSkills)
                          .ThenInclude(ss => ss.Skill)
                          .Include(s => s.PlacementRecords)
                          .FirstOrDefaultAsync(s => s.Id == studentId, ct)
                      ?? throw new NotFoundException($"Student {studentId} not found.");

        var jobPosting = await context.JobPostings
                             .Include(jp => jp.RequiredSkills)
                             .ThenInclude(js => js.Skill)
                             .FirstOrDefaultAsync(jp => jp.Id == jobPostingId, ct)
                         ?? throw new NotFoundException($"Job posting {jobPostingId} not found.");

        var requiredJobSkills = jobPosting.RequiredSkills.Where(js => js.IsRequired).ToList();
        var studentSkillIds = student.StudentSkills.Select(ss => ss.SkillId).ToHashSet();

        var totalRequired = requiredJobSkills.Count;
        var matchedRequiredCount = requiredJobSkills.Count(js => studentSkillIds.Contains(js.SkillId));

        var requiredRatio = totalRequired == 0 ? 1.0f : (float)matchedRequiredCount / totalRequired;
        var gwaFactor = student.Gwa > 0 ? (float)Math.Clamp((3.0m - student.Gwa) / 2.0m, 0.0m, 1.0m) : 0.7f;
        var proficiencyDepth = student.StudentSkills.Count > 0
            ? (float)student.StudentSkills.Average(ss => ss.ProficiencyLevel)
            : 1.0f;
        var certCount = student.StudentSkills.Count;
        var internshipCount = student.PlacementRecords.Count;

        var input = new JobMatchInput
        {
            RequiredSkillRatio = requiredRatio,
            GwaScore = gwaFactor,
            ProficiencyDepth = proficiencyDepth,
            CertificationCount = certCount,
            InternshipCount = internshipCount
        };

        var predictionEngine = mlContext.Model.CreatePredictionEngine<JobMatchInput, JobMatchPrediction>(_model);
        var prediction = predictionEngine.Predict(input);

        var matchScore = Math.Round(Math.Clamp(prediction.Score * 100.0, 0.0, 100.0), 2);

        string[] featureNames =
        [
            "Required Skill Ratio", "Academic Standing (GWA)", "Proficiency Depth", "Certifications",
            "Internships"
        ];
        var contributions = new List<FeatureContribution>();

        if (prediction.FeatureContributions.Length >= featureNames.Length)
        {
            for (var i = 0; i < featureNames.Length; i++)
            {
                var contribVal = prediction.FeatureContributions[i];
                contributions.Add(new FeatureContribution
                {
                    FeatureName = featureNames[i],
                    Value = Math.Round(contribVal * 100.0, 2),
                    Importance = Math.Round(Math.Abs(contribVal), 4),
                    HumanReadableImpact = contribVal >= 0 ?
                        "Positively impacted model match score."
                        : "Negatively impacted model match score."
                });
            }
        }

        var missingSkills = requiredJobSkills
            .Where(js => !studentSkillIds.Contains(js.SkillId))
            .Select(js => js.Skill?.Name ?? "Unknown Skill")
            .ToList();

        var requiredSkillIdSet = requiredJobSkills.Select(js => js.SkillId).ToHashSet();
        var strengths = student.StudentSkills
            .Where(ss => requiredSkillIdSet.Contains(ss.SkillId))
            .Select(ss => $"Matched required skill: {ss.Skill?.Name}")
            .ToList();

        return new XaiExplanation
        {
            MatchScore = matchScore,
            FeatureContributions = contributions,
            Summary = matchScore >= 70.0
                ? "ML.NET model predicts strong candidate fit."
                : "ML.NET model predicts moderate fit; skill gap closure advised",
            Strengths = strengths.Count > 0 ? strengths : ["Foundational profile registered."],
            Weaknesses = [.. missingSkills.Select(s => $"Missing required skills: {s}")],
            Recommendations = [.. missingSkills.Select(s => $"Acquire targeted experience in {s}")]
        };
    }

    private static ITransformer InitializeOrTrainDefaultModel(MLContext mlContext)
    {
        var dummyTrainingData = new List<JobMatchInput>
        {
            new()
            {
                RequiredSkillRatio = 1.0f,
                GwaScore = 0.9f,
                ProficiencyDepth = 3.0f,
                CertificationCount = 2.0f,
                InternshipCount = 1.0f,
                Label = 0.95f
            },

            new()
            {
                RequiredSkillRatio = 0.2f,
                GwaScore = 0.4f,
                ProficiencyDepth = 1.0f,
                CertificationCount = 0.0f,
                InternshipCount = 0.0f,
                Label = 0.30f
            },

            new()
            {
                RequiredSkillRatio = 0.8f,
                GwaScore = 0.7f,
                ProficiencyDepth = 2.5f,
                CertificationCount = 1.0f,
                InternshipCount = 1.0f,
                Label = 0.85f
            }
        };

        var dataView = mlContext.Data.LoadFromEnumerable(dummyTrainingData);

        var pipeline = mlContext.Transforms.Concatenate("Features",
                nameof(JobMatchInput.RequiredSkillRatio),
                nameof(JobMatchInput.GwaScore),
                nameof(JobMatchInput.ProficiencyDepth),
                nameof(JobMatchInput.CertificationCount),
                nameof(JobMatchInput.InternshipCount))
            .Append(mlContext.Regression.Trainers.FastTree(
                labelColumnName: nameof(JobMatchInput.Label),
                featureColumnName: "Features",
                numberOfLeaves: 5,
                numberOfTrees: 20));

        return pipeline.Fit(dataView);
    }
}