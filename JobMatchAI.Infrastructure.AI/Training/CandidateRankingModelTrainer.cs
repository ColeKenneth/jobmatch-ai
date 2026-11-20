using JobMatchAI.Infrastructure.AI.Data;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers.FastTree;

namespace JobMatchAI.Infrastructure.AI.Training;

public class CandidateRankingModelTrainer(MLContext mlContext)
{
    private static readonly string[] FeatureColumns =
    [
        nameof(CandidateFeatureInput.Gwa),
        nameof(CandidateFeatureInput.GraduationYear),
        nameof(CandidateFeatureInput.SkillMatchScore),
        nameof(CandidateFeatureInput.CertificationCount),
        nameof(CandidateFeatureInput.InternshipCount),
        nameof(CandidateFeatureInput.DegreeRelevanceScore)
    ];

    public CalibratedBinaryClassificationMetrics TrainAndSave(
        IEnumerable<CandidateFeatureInput> trainingExamples,
        string outputModelPath,
        int? seed = 42
    )
    {
        ArgumentNullException.ThrowIfNull(trainingExamples);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputModelPath);

        var materialized = trainingExamples.ToList();
        if (materialized.Count == 0)
        {
            throw new ArgumentException("Training examples cannot be empty.", nameof(trainingExamples)); 
        }

        var fullData = mlContext.Data.LoadFromEnumerable(materialized);
        var split = mlContext.Data.TrainTestSplit(fullData, testFraction: 0.2, seed: seed);

        var options = new FastForestBinaryTrainer.Options
        {
            LabelColumnName = "Label",
            FeatureColumnName = "Features",
            NumberOfTrees = 100,
            FeatureFraction = 0.8,
            FeatureFirstUsePenalty = 0.05,
            MinimumExampleCountPerLeaf = 5
        };

        var pipeline = mlContext.Transforms
            .Concatenate("Features", FeatureColumns)
            .Append(mlContext.BinaryClassification.Trainers.FastForest(options));

        var trainedModel = pipeline.Fit(split.TrainSet);

        var scoredTest = trainedModel.Transform(split.TestSet);
        var metrics = mlContext.BinaryClassification.Evaluate(
            scoredTest,
            labelColumnName: "Label",
            scoreColumnName: "Score",
            probabilityColumnName: "Probability",
            predictedLabelColumnName: "PredictedLabel"
        );

        var directory = Path.GetDirectoryName(outputModelPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        mlContext.Model.Save(trainedModel, fullData.Schema, outputModelPath);

        return metrics;
    }
}