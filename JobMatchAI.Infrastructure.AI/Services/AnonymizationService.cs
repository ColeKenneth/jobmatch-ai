using System.Text.RegularExpressions;
using ManagedCode.Presidio.Analyzer;
using ManagedCode.Presidio.Anonymizer;


namespace JobMatchAI.Infrastructure.AI.Services;

public partial class AnonymizationService(AnalyzerEngine analyzerEngine, AnonymizerEngine anonymizerEngine) : IAnonymizationService
{
    
    public string? ScrubPii(string? rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return string.Empty;
        }

        var analysisResults = analyzerEngine.Analyze(rawText, "en");
        var anonymizationResult = anonymizerEngine.Anonymize(rawText, analysisResults);

        var scrubbedText = PhilippinePhoneRegex().Replace(anonymizationResult.Text ?? "", "<PHONE_NUMBER>");

        return anonymizationResult.Text;
    }

    [GeneratedRegex(@"(?:\+?63|0)?9\d{2}[-.\s]?\d{3}[-.\s]?\d{4}\b")]
    private static partial Regex PhilippinePhoneRegex();
}