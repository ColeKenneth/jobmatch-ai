using JobMatchAI.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace JobMatchAI.Application.DTOs
{
    public record UserDto
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "First name is required.")]
        [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters.")]
        public string FirstName { get; init; } = string.Empty;

        [StringLength(100, ErrorMessage = "Middle name cannot exceed 100 characters.")]
        public string? MiddleName { get; init; }

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters.")]
        public string LastName { get; init; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [StringLength(255, ErrorMessage = "Email address cannot exceed 255 characters.")
        public string Email { get; init; } = string.Empty;

        [EnumDataType(typeof(UserRole), ErrorMessage = "Invalid user role.")]
        public UserRole Role { get; init; }

        public DateTime CreatedAt { get; init; }

        public bool IsActive { get; init; }

        public string FullName => string.IsNullOrWhiteSpace(MiddleName)
            ? $"{FirstName} {LastName}" : $"{FirstName} {MiddleName} {LastName}";
    }
}
