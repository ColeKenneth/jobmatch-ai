using System;
using System.Collections.Generic;
using System.Text;

namespace JobMatchAI.Domain.Exceptions
{
    public class AlreadyExistsException : DomainException
    {
        public AlreadyExistsException(string entity, string field, string value)
        : base($"{entity} with {field} '{value}' already exists.") { }

        public AlreadyExistsException(string message) : base(message) { }
    }
}
