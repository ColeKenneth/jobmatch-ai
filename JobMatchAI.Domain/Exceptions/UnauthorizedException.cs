namespace JobMatchAI.Domain.Exceptions
{
    public class UnauthorizedException(string message) : DomainException(message)
    { }
}
