using System;
using System.Collections.Generic;
using System.Text;

namespace JobMatchAI.Domain.Exceptions
{
    public abstract class DomainException : Exception
    {
        protected DomainException(string message) : base(message) { }
        protected DomainException(string message, Exception inner) : base(message, inner) { }
    }
}
