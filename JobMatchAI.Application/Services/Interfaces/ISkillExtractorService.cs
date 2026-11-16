namespace JobMatchAI.Application.Services.Interfaces;

public interface ISkillExtractorService
{
    Task<List<string>> ExtractSkillAsync(string resumeText);
}