using System.ComponentModel.DataAnnotations;

namespace JobMatchAI.Application.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public sealed class PastOrPresentAttribute : ValidationAttribute
    {
        public PastOrPresentAttribute()
        {
            ErrorMessage = "{0} cannot be a future date.";
        }

        public override bool IsValid(object? value)
        {
            if (value is null) return true;

            return value switch
            {
                DateTime dt => dt.ToUniversalTime() <= DateTime.UtcNow,
                DateTimeOffset dto => dto.UtcDateTime <= DateTime.UtcNow,
                _ => false
            };
        }
    }
}
