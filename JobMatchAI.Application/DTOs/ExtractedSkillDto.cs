namespace JobMatchAI.Application.DTOs;

public record ExtractedSkillDto(
    string SkillName,
    string Category,
    int ProficiencyLevel
    );