using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace JobMatchAI.Application.DTOs
{
    public record EmployerDto
    {
        public Guid Id { get; init; }

        public Guid UserId { get; init; }

        [Required(ErrorMessage = "Company name is required.")]
        [StringLength(80, ErrorMessage = "Company name cannot exceed 80 characters.")]
        public string CompanyName { get; init; } = string.Empty;

        [Required(ErrorMessage = "Industry is required.")]
        [StringLength(80, ErrorMessage = "Industry name cannot exceed 80 characters.")]
        public string Industry { get; init; } = string.Empty;

        [StringLength(500, ErrorMessage = "Website cannot exceed 500 characters.")]
        public string? Website { get; init; }

        [StringLength(1000, ErrorMessage = "Company description cannot exceed 1000 characters.")]
        public string? CompanyDescription { get; init; }

        [StringLength(500, ErrorMessage = "Logo URL cannot exceed 500 characters.")]
        public string? LogoUrl { get; init; }
    }
}
