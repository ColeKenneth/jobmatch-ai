using JobMatchAI.Application.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace JobMatchAI.Application.DTOs
{
    public record PlacementAnalysis
    {
        [NonNegativeNumber]
        public int TotalStudents { get; init; }

        [NonNegativeNumber]
        public int PlacedStudents { get; init; }

        [NonNegativeNumber]
        public int InternshipPlacements { get; init; }

        [NonNegativeNumber]
        public double PlacementRate { get; init; }

        [NonNegativeNumber]
        public int AverageDaysToPlacement { get; init; }

        public IReadOnlyList<ProgramAnalytics> ProgramBreakdown { get; init; } = [];

        public IReadOnlyDictionary<string, double> IndustryDistribution { get; init; } = new Dictionary<string, double>();
    }
}
