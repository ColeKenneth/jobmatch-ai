using JobMatchAI.Application.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace JobMatchAI.Infrastructure.AI.Services;

public class SkillExtractorService : ISkillExtractorService
{
    private readonly Kernel _kernel;
    
    public SkillExtractorService(IConfiguration config)
    {
        var builder = Kernel.CreateBuilder();

        builder.AddGoogleAIGeminiChatCompletion(
            modelId: config["AI:ModelId"] ?? "gemini-1.5-flash",
            apiKey: config["AI:ApiKey"]!
        );
        
        _kernel =  builder.Build();
    }

    public async Task<List<string>> ExtractSkillAsync(string resumeText)
    {
        var prompt = $"""
                      You are an expert professional recruiter and career analyst.
                      Read the following resume text and extract a comprehensive list of professional competencies, technical skills,
                      software, tools, domain expertise, and certifications. Include relevant hard skills and industry-specific
                      proficiencies, but DO NOT include generic soft skills (like "teamwork", "hardworking", or "communication").
                      Return ONLY a valid JSON array of strings. NO MARKDOWN formatting, NO EXTRA TEXT.

                      Example Output (Tech): ["C#", "ASP.NET Core", "PostgreSQL", "Kafka"]
                      Example Output (Non-tech): ["Financial Analysis", "SAP ERP", "Tax Compliance", "Microsoft Office", "Auditing"]

                      Resume:
                      {resumeText}
                      """;

        var result = await _kernel.InvokePromptAsync(prompt);
        var responseText = result.ToString().Trim();

        if (responseText.StartsWith("```json"))
        {
            responseText = responseText.Replace("```json", "").Replace("```", "").Trim();
        }

        try
        {
            var skills = JsonSerializer.Deserialize<List<string>>(responseText);
            return skills ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}