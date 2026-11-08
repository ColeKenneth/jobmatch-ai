namespace JobMatchAI.Application.Services.Interfaces;

public interface IResumeParserService
{
    Task<string> ExtractTextAsync(Stream fileStream, string fileName);
}