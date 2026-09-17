using JobMatchAI.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace JobMatchAI.Application.DTOs
{
    public record SkillDto
    {
        public Guid Id { get; init; }

        [Required(ErrorMessage = "Your skill name is required.")]
        [StringLength(100, ErrorMessage = "Skill name cannot exceed 100 characters.")]
        public string Name { get; init; } = string.Empty;

        [EnumDataType(typeof(Category), ErrorMessage = "Invalid category.")]
        public Category Category { get; init; }

        [StringLength(100, ErrorMessage = "Ontology ID cannot exceed 100 characters.")]
        public string? OntologyId { get; init; }

        public string? Description { get; init; }

        [StringLength(500, ErrorMessage = "Icon URL cannot exceed 500 characters.")]
        [Url(ErrorMessage = "Icon URL must be a valid URL.")]
        public string? IconUrl { get; init; }
    }
}
