using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace JobMatchAI.Application.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public sealed class FutureDateAttribute : ValidationAttribute
    {
        public FutureDateAttribute()
        {
            ErrorMessage = $"{0} must be a future date.";
        }

        public override bool IsValid(object? value)
        {
            return value switch
            {
                DateTime dt => dt.ToUniversalTime() >= DateTime.UtcNow,
                DateTimeOffset dto => dto.UtcDateTime >= DateTime.UtcNow,
                _ => false
            };
        }
    }
}
