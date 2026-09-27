using JobMatchAI.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace JobMatchAI.Application.DTOs
{
    public record StudentDto
    {
        public Guid Id { get; init; }

        public Guid UserId { get; init; }

        [Required(ErrorMessage = "Student ID number is required.")]
        [RegularExpression(@"^\d{2}-[A-Z]{2}\d{5}$", ErrorMessage = "Student ID number format must follow this format: 00-AB12345")]
        [StringLength(30, ErrorMessage = "Student ID number cannot exceed 30 characters.")]
        public string StudentIdNumber { get; init; } = string.Empty;

        [Required(ErrorMessage = "Program is required.")]
        [StringLength(100, ErrorMessage = "Program name cannot exceed 100 characters.")]
        public string Program { get; init; } = string.Empty;

        [EnumDataType(typeof(YearLevel), ErrorMessage = "Invalid year level.")]
        public YearLevel YearLevel { get; init; }

        [Range(1.00, 5.00, ErrorMessage = "GWA must be between 1.00 and 5.00")]
        public decimal Gwa { get; init; }

        public DateOnly? GraduationDate { get; init; }

        [StringLength(100, ErrorMessage = "Resume file path cannot exceed 100 characters.")]
        public string? ResumeFilePath { get; init; }

        public string? ResumeText { get; init; }
    }
}
