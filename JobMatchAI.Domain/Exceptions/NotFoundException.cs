using System;
using System.Collections.Generic;
using System.Text;

namespace JobMatchAI.Domain.Exceptions
{
    public class NotFoundException : DomainException
    {
        public NotFoundException(string entity, Guid id) 
            : base($"{entity} with ID {id} was not found.") { }

        public NotFoundException(string message) : base(message) { }
    }
}
