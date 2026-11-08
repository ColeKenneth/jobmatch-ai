using System.ComponentModel.DataAnnotations;

namespace JobMatchAI.Application.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public sealed class FutureDateAttribute : ValidationAttribute 
    {
        public FutureDateAttribute()
        {
            ErrorMessage = "{0} cannot happen in the past.";
        }

        public override bool IsValid(object? value)
        {
            if (value is null) return true;

            return value switch
            {
                DateTime dt => dt.ToUniversalTime() >= DateTime.UtcNow,
                DateTimeOffset dto => dto.UtcDateTime >= DateTime.UtcNow,
                _ => false
            };
        }
    }
}
