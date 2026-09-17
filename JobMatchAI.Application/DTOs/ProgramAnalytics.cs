using JobMatchAI.Application.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace JobMatchAI.Application.DTOs
{
    public record ProgramAnalytics
    {
        [Required(ErrorMessage = "Program name is required.")]
        [StringLength(100, ErrorMessage = "Program name cannot exceed 100 characters.")]
        public string ProgramName { get; init; } = string.Empty;

        [NonNegativeNumber]
        public int TotalStudents { get; init; }

        [NonNegativeNumber]
        public int Placed { get; init; }

        [Range(0.00, 100.00, ErrorMessage = "Placement rate is between 0% and 100%.")]
        public double PlacementRate { get; init; }
    }
}
