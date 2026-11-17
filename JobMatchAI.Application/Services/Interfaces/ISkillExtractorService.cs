using JobMatchAI.Application.DTOs;

namespace JobMatchAI.Application.Services.Interfaces;

public interface ISkillExtractorService
{
    Task<ExtractedResumeDataDto> ExtractSkillsAndMetadataAsync(string resumePlainText, CancellationToken ct = default);
}