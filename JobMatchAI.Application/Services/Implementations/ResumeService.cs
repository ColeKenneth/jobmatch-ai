using JobMatchAI.Application.DTOs;
using JobMatchAI.Application.Services.Interfaces;
using JobMatchAI.Domain.Entities;
using JobMatchAI.Domain.Exceptions;
using JobMatchAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobMatchAI.Application.Services.Implementations;

public class ResumeService(
    IResumeParserService resumeParserService,
    ISkillExtractorService skillExtractorService,
    AppDbContext context,
    ILogger<ResumeService> logger) : IResumeService
{
    public async Task<ResumeExtractionResponse> ProcessAndSaveResumeAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        var resumeText = await resumeParserService.ExtractTextAsync(fileStream, fileName);

        if (string.IsNullOrWhiteSpace(resumeText))
        {
            throw new InvalidOperationException("Could not extract text from the provided document.");
        }

        var extractedData = await skillExtractorService.ExtractSkillsAndMetadataAsync(resumeText, ct);

        var candidateResume = new CandidateResume
        {
            FileName = fileName,
            RawText = resumeText,
            UploadedAt = DateTime.UtcNow,
            Skills = 
            [
                .. extractedData.ExtractedSkills.Select(skill => new CandidateSkill
                {
                    SkillName = skill.SkillName,
                })
            ]
        };

        context.CandidateResumes.Add(candidateResume);
        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Failed to process and save resume {fileName}", fileName);
            throw new BusinessRuleException("Failed to process and save resume.");
        }

        return new ResumeExtractionResponse(
            FileName: fileName,
            ExtractedSkills: [.. extractedData.ExtractedSkills.Select(s => s.SkillName)],
            TotalSkillsFound: extractedData.ExtractedSkills.Count
        );
    }
}