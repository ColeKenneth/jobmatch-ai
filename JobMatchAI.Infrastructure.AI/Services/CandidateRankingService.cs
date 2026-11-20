using JobMatchAI.Infrastructure.AI.Data;
using Microsoft.ML;

namespace JobMatchAI.Infrastructure.AI.Services;

public sealed class CandidateRankingService : ICandidateRankingService, IDisposable
{
    private readonly MLContext _mlContext;
    private readonly ITransformer _trainedModel;
    private readonly Lock _syncLock = new();
    private readonly PredictionEngine<CandidateFeatureInput, CandidatePredictionOutput> _predictionEngine;

    public CandidateRankingService(string modelPath)
    {
        var mlContext = new MLContext();

        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException($"The trained ML model file was not found at: {modelPath}");
        }

        _mlContext = new MLContext();
        _trainedModel = _mlContext.Model.Load(modelPath, out _);
        _predictionEngine = mlContext.Model.CreatePredictionEngine<CandidateFeatureInput, CandidatePredictionOutput>(_trainedModel);
    }

    public CandidatePredictionOutput EvaluateCandidate(CandidateFeatureInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        
        lock (_syncLock)
        {
            return  _predictionEngine.Predict(input);
        }
        
    }

    public void Dispose()
    {
        lock (_syncLock)
        {
            _predictionEngine.Dispose();
        }
    }
}