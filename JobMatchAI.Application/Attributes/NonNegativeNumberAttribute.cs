using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace JobMatchAI.Application.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public sealed class NonNegativeNumberAttribute : ValidationAttribute
    {
        public NonNegativeNumberAttribute()
        {
            ErrorMessage = $"{0} must be a non-negative number.";
        }

        public override bool IsValid(object? value)
        {
            return value is >= 0;
        }
    }
}
