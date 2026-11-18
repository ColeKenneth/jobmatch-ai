using JobMatchAI.Infrastructure.AI.Data;
using Microsoft.ML;

namespace JobMatchAI.Infrastructure.AI.Services;

public sealed class CandidateRankingService : ICandidateRankingService, IDisposable
{
    private readonly PredictionEngine<CandidateFeatureInput, CandidatePredictionOutput> _predictionEngine;

    public CandidateRankingService(string modelPath)
    {
        var mlContext = new MLContext();

        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException($"The trained ML model file was not found at: {modelPath}");
        }

        var trainedModel = mlContext.Model.Load(modelPath, out var modelSchema);
        _predictionEngine = mlContext.Model.CreatePredictionEngine<CandidateFeatureInput, CandidatePredictionOutput>(trainedModel);
    }

    public CandidatePredictionOutput EvaluateCandidate(CandidateFeatureInput input)
    {
        return  _predictionEngine.Predict(input);
    }

    public void Dispose()
    {
        _predictionEngine.Dispose();
    }
}