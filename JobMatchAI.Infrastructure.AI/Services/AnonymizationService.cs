using ManagedCode.Presidio.Analyzer;
using ManagedCode.Presidio.Anonymizer;

namespace JobMatchAI.Infrastructure.AI.Services;

public class AnonymizationService(AnalyzerEngine analyzerEngine, AnonymizerEngine anonymizerEngine) : IAnonymizationService
{
    public string? ScrubPii(string? rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return string.Empty;
        }

        var analysisResults = analyzerEngine.Analyze(rawText, "en");
        var anonymizationResult = anonymizerEngine.Anonymize(rawText, analysisResults);

        return anonymizationResult.Text;
    }
}