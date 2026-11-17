namespace JobMatchAI.Application.DTOs;

public record ResumeExtractionResponse(
    string FileName,
    List<string> ExtractedSkills,
    int TotalSkillsFound
    );