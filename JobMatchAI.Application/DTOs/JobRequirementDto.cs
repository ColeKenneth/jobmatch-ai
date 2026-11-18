using System.ComponentModel.DataAnnotations;

namespace JobMatchAI.Application.DTOs;

public record JobRequirementDto(
    [Required(ErrorMessage = "Job title is required.")]
    string JobTitle,
    IReadOnlyList<string> RequiredSkills,
    IReadOnlyList<string> TargetDegreePrograms
    );