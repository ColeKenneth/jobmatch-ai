using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace JobMatchAI.Application.DTOs
{
    public record CertificationDto
    {
        [Required(ErrorMessage = "Certification name is required.")]
        [StringLength(maximumLength: 100, ErrorMessage = "Certification name cannot exceed 100 characters.")]
        public string Name { get; init; } = string.Empty;

        [Required(ErrorMessage = "Issuing authority is required.")]
        [StringLength(maximumLength: 100, ErrorMessage = "Issuing authority cannot exceed 100 characters.")]
        public string IssuingAuthority { get; init; } = string.Empty;

        [StringLength(maximumLength: 500, ErrorMessage = "Verification URL cannot exceed 500 characters.")]
        public string? VerificationUrl { get; init; }
    }
}
