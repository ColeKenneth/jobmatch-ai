using System.ComponentModel.DataAnnotations;

namespace JobMatchAI.Application.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public sealed class NonNegativeNumberAttribute : ValidationAttribute
    {
        public NonNegativeNumberAttribute()
        {
            ErrorMessage = "{0} must be a non-negative number.";
        }

        public override bool IsValid(object? value)
        {
            if (value is null) return true;
            return value is >= 0;
        }
    }
}
