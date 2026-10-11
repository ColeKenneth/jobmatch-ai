using JobMatchAI.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace JobMatchAI.Application.DTOs
{
    public record PlacementRecordDto
    {
        public Guid Id { get; init; }

        public Guid StudentId { get; init; }

        public Guid JobPostingId { get; init; }

        [Required(ErrorMessage = "Placement date is required.")]
        public DateTime PlacementDate { get; init; }

        [EnumDataType(typeof(PlacementStatus), ErrorMessage = "Invalid placement status.")]
        public PlacementStatus PlacementStatus { get; init; }

        [StringLength(2000, ErrorMessage = "Notes cannot exceed 2000 characters.")]
        public string? Notes { get; init; }
    }
}