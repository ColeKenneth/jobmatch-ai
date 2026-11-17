using JobMatchAI.Application.DTOs;
using JobMatchAI.Application.Services.Interfaces;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;

namespace JobMatchAI.Infrastructure.AI.Services;

public class SkillExtractorService(Kernel kernel) : ISkillExtractorService
{
    public async Task<ExtractedResumeDataDto> ExtractSkillsAndMetadataAsync(string resumePlainText, CancellationToken ct = default)
    {
        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        
        var dynamicCurrentYear = DateTime.UtcNow.Year;
        var chatHistory = new ChatHistory();

        var systemPrompt = """
            You are an advanced AI career intelligence parser for a Philippine higher education institution. 
            Your task is to analyze unstructured resume text across any discipline (Information Technology, Business Administration, Allied Health, Engineering, etc.) for both current students and alumni.
            
            Extract the structured data below and return ONLY valid JSON matching this schema:
            {{
              "gwa": 0.0,
              "graduationYear": {dynamicCurrentYear},
              "degreeProgram": "string",
              "extractedSkills": [
                {{
                  "skillName": "string",
                  "category": "string",
                  "proficiencyLevel": 3
                }}
              ],
              "certificationCount": 0,
              "internshipCount": 0
            }}
            
            Guidelines:
            - Extract the actual 4-digit graduation year found in the resume. If multiple degrees exist, use the most recent one. If none found, set to 0.
            - Normalize skill names (e.g., "MS Excel" -> "Microsoft Excel", "JS" -> "JavaScript").
            - Estimate a proficiency level from 1 to 5 based on context (years of experience, project complexity, or role seniority).
            - If GWA is not explicitly found, set it to 0.0.
            - Accurately count total distinct certifications and internships listed.
            """;

        systemPrompt = systemPrompt.Replace("{dynamicCurrentYear}", dynamicCurrentYear.ToString());

        chatHistory.AddSystemMessage(systemPrompt);
        chatHistory.AddUserMessage($"Here is the resume text to analyze:\n\n{resumePlainText}");

        var result = await chatService.GetChatMessageContentAsync(chatHistory, cancellationToken: ct);
        var jsonResponse = result.Content?.Trim() ?? "{}";

        if (jsonResponse.StartsWith("```json"))
        {
            jsonResponse = jsonResponse.Replace("```json", "").Replace("```", "").Trim();
        }
        else if (jsonResponse.StartsWith("```"))
        {
            jsonResponse = jsonResponse.Replace("```", "").Trim();
        }

        try
        {
            return JsonSerializer.Deserialize<ExtractedResumeDataDto>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new ExtractedResumeDataDto(0.0m, 0, string.Empty, [], 0, 0);
        }
        catch (JsonException)
        {
            return new ExtractedResumeDataDto(0.0m,  0, string.Empty, [], 0, 0);
        }
    }
}