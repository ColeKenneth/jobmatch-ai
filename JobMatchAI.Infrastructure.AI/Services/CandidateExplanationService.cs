using System.Collections;
using JobMatchAI.Infrastructure.AI.Data;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;

namespace JobMatchAI.Infrastructure.AI.Services;

public sealed class CandidateExplanationService : IDisposable
{
    private static readonly string[] FeatureNames =
    [
        nameof(CandidateFeatureInput.Gwa),
        nameof(CandidateFeatureInput.GraduationYear),
        nameof(CandidateFeatureInput.SkillMatchScore),
        nameof(CandidateFeatureInput.CertificationCount),
        nameof(CandidateFeatureInput.InternshipCount),
        nameof(CandidateFeatureInput.DegreeRelevanceScore)
    ];

    private readonly MLContext _mlContext;
    private readonly ITransformer _featurePrepTransformer;
    private readonly ISingleFeaturePredictionTransformer<ICalculateFeatureContribution> _predictor;
    private readonly ITransformer _contributionTransformer;
    private readonly Lock _syncLock = new();
    private bool _disposed;

    public CandidateExplanationService(string modelPath, IEnumerable<CandidateFeatureInput> calibrationSamples)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelPath);
        ArgumentNullException.ThrowIfNull(calibrationSamples);

        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException($"Trained model file not found at: {modelPath}");
        }

        var samples = calibrationSamples.ToList();
        if (samples.Count == 0)
        {
            throw new ArgumentException("At least one sample is required to fit the feature contribution transformer.",
                nameof(calibrationSamples));
        }
        
        _mlContext = new MLContext();
        var loadedModel = _mlContext.Model.Load(modelPath, out _);

        _predictor = FindFeatureContributionTransformer(loadedModel)
                        ?? throw new InvalidOperationException(
                            "Loaded model does not contain a supported feature contribution transformer.");
        var prepEstimator = _mlContext.Transforms.Concatenate("Features", FeatureNames);
        var sampleView = _mlContext.Data.LoadFromEnumerable(samples);
        _featurePrepTransformer = prepEstimator.Fit(sampleView);
        var preparedSamples = _featurePrepTransformer.Transform(sampleView);
        var scoredSamples = _predictor.Transform(preparedSamples);

        var contributionEstimator = _mlContext.Transforms.CalculateFeatureContribution(_predictor);
        _contributionTransformer = contributionEstimator.Fit(scoredSamples);
    }

    public CandidateRankingExplanation Explain(CandidateFeatureInput input)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(input);

        lock (_syncLock)
        {
            var singleRow = _mlContext.Data.LoadFromEnumerable([input]);
            var prepared = _featurePrepTransformer.Transform(singleRow);
            var scored = _predictor.Transform(prepared);
            var explained = _contributionTransformer.Transform(scored);

            var row = _mlContext.Data
                .CreateEnumerable<FeatureContributionRow>(explained, reuseRowObject: false)
                .First();

            float[] featureValues =
            [
                input.Gwa,
                input.GraduationYear,
                input.SkillMatchScore,
                input.CertificationCount,
                input.InternshipCount,
                input.DegreeRelevanceScore
            ];
            
            var contributions = row.FeatureContributions ?? [];
            var attributions = new List<FeatureAttributions>(FeatureNames.Length);

            for (var i = 0; i < FeatureNames.Length; i++)
            {
                var contrib = i < contributions.Length ? contributions[i] : 0f;
                attributions.Add(new FeatureAttributions(FeatureNames[i], featureValues[i], contrib));
            }

            IReadOnlyList<FeatureAttributions> sorted =
            [
                .. attributions
                    .OrderByDescending(a => Math.Abs(a.Contribution))
            ];

            var prediction = new CandidatePredictionOutput
            {
                PredictedLabel = row.PredictedLabel,
                Score = row.Score,
                Probability = row.Probability
            };

            return new CandidateRankingExplanation(prediction, sorted);
        }
    }
    
    public void Dispose()
    {
        lock (_syncLock)
        {
            _disposed = true;
        }
    }

    private static ISingleFeaturePredictionTransformer<ICalculateFeatureContribution>?
        FindFeatureContributionTransformer(ITransformer transformer)
    {
        return transformer switch
        {
            ISingleFeaturePredictionTransformer<ICalculateFeatureContribution> direct => direct,
            IEnumerable stages => stages.Cast<object>()
                .OfType<ITransformer>()
                .Select(FindFeatureContributionTransformer)
                .OfType<ISingleFeaturePredictionTransformer<ICalculateFeatureContribution>>()
                .FirstOrDefault(),
            _ => null
        };
    }

    private sealed class FeatureContributionRow : CandidatePredictionOutput
    {
        [ColumnName("FeatureContributions")]
        public float[]? FeatureContributions { get; set; }
    }
}