using System;
using System.Collections.Generic;
using System.Text;

namespace JobMatchAI.Domain.Exceptions
{
    public class UnauthorizedException(string message) : DomainException(message)
    { }
}
