namespace JobMatchAI.Application.DTOs;

public record ExtractedResumeDataDto(
    decimal Gwa,
    int GraduationYear,
    string DegreeProgram,
    List<ExtractedSkillDto> ExtractedSkills,
    int CertificationCount,
    int InternshipCount
    );