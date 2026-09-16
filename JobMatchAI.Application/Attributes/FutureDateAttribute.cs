using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace JobMatchAI.Application.Attributes
{
    public sealed class FutureDateAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            return value is DateTime date && date >= DateTime.UtcNow;
        }
    }
}
