using JobMatchAI.Application.DTOs;

namespace JobMatchAI.Application.Services.Interfaces;

public interface IResumeService
{
    Task<ResumeExtractionResponse> ProcessAndSaveResumeAsync(Stream fileStream, string fileName,
        CancellationToken ct = default);
}