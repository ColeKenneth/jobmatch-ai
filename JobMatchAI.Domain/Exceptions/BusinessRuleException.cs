using System;
using System.Collections.Generic;
using System.Text;

namespace JobMatchAI.Domain.Exceptions
{
    public class BusinessRuleException(string message) : DomainException(message) { }
}
