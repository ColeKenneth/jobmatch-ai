namespace JobMatchAI.Domain.Exceptions
{
    public record ErrorResponse(
        int Status,
        string Error,
        object Message,
        DateTime Timestamp
        );
}
