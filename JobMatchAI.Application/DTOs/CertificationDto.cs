using JobMatchAI.Application.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace JobMatchAI.Application.DTOs
{
    public record CertificationDto
    {
        public Guid Id { get; init; }

        [Required(ErrorMessage = "Certification name is required.")]
        [StringLength(100, ErrorMessage = "Certification name cannot exceed 100 characters.")]
        public string Name { get; init; } = string.Empty;

        [Required(ErrorMessage = "Issuing authority is required.")]
        [StringLength(100, ErrorMessage = "Issuing authority cannot exceed 100 characters.")]
        public string IssuingAuthority { get; init; } = string.Empty;

        [PastOrPresent(ErrorMessage = "Issued date cannot happen in the future.")]
        public DateTime DateIssued { get; init; }

        [FutureDate(ErrorMessage = "Expiration date cannot happen in the past.")]
        public DateTime? ExpiryDate { get; init; }

        [StringLength(500, ErrorMessage = "Verification URL cannot exceed 500 characters.")]
        [Url(ErrorMessage = "Verification URL must be a valid URL.")]
        public string? VerificationUrl { get; init; }

        [StringLength(50, ErrorMessage = "Credential ID cannot exceed 50 characters.")]
        public string? CredentialId { get; init; }
    }
}
