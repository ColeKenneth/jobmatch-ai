using JobMatchAI.Application.Services.Interfaces;
using JobMatchAI.Domain.Entities;
using JobMatchAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobMatchAI.Application.Services.Implementations;

public class SkillNormalizationService(ILogger<SkillNormalizationService> logger, AppDbContext context) : ISkillNormalizationService
{
    public async Task NormalizeAndLinkSkillAsync(Guid studentId, IEnumerable<string> rawSkillNames, CancellationToken ct = default)
    {
        var student = await context.Students
            .Include(s => s.StudentSkills)
            .FirstOrDefaultAsync(s => s.Id == studentId, ct);

        if (student == null) return;

        foreach (var rawName in rawSkillNames)
        {
            var cleanedLower = rawName.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(cleanedLower))
            {
                continue;
            }

            var canonicalSkill = await context.Skills
                .FirstOrDefaultAsync(s => s.Name.ToLower() == cleanedLower, ct);

            if (canonicalSkill != null) continue;
            canonicalSkill = new Skill
            {
                Name = cleanedLower,
            };
                
            context.Skills.Add(canonicalSkill);
            await context.SaveChangesAsync(ct);

            var existingLink = student.StudentSkills
                .FirstOrDefault(ss => ss.SkillId == canonicalSkill.Id);

            if (existingLink != null) continue;

            context.StudentSkills.Add(new StudentSkill
            {
                StudentId = student.Id,
                SkillId = canonicalSkill.Id,
                ProficiencyLevel = 2
            });
        }

        await context.SaveChangesAsync(ct);
        logger.LogInformation("Normalized and linked skills for student {StudentId}", studentId);
    }
}