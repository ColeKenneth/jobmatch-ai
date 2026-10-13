using JobMatchAI.Domain.Exceptions;
using System.Net;

namespace JobMatchAI.Middleware
{
    public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (NotFoundException ex)
            {
                logger.LogWarning("Resource not found: {Message}", ex.Message);
                await WriteErrorAsync(context, HttpStatusCode.NotFound, "Not Found", ex.Message);
            }
            catch (AlreadyExistsException ex)
            {
                logger.LogWarning("Duplicated resource: {Message}", ex.Message);
                await WriteErrorAsync(context, HttpStatusCode.Conflict, "Conflict", ex.Message);
            }
            catch (BusinessRuleException ex)
            {
                logger.LogWarning("Business rule violation: {Message}", ex.Message);
                await WriteErrorAsync(context, HttpStatusCode.BadRequest, "Business Rule Validation", ex.Message);
            }
            catch (UnauthorizedException ex)
            {
                logger.LogWarning("Unauthorized: {Message}", ex.Message);
                await WriteErrorAsync(context, HttpStatusCode.Unauthorized, "Unauthorized", ex.Message);
            }
            catch (DomainException ex)
            {
                logger.LogWarning("Domain error: {Message}", ex.Message);
                await WriteErrorAsync(context, HttpStatusCode.BadRequest, "Bad Request", ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception at {Path}", context.Request.Path);
                await WriteErrorAsync(context, HttpStatusCode.InternalServerError,
                    "Internal Server Error",
                    "An unexpected error occurred.");
            }
        }

        private static async Task WriteErrorAsync(HttpContext context, HttpStatusCode status, string error, object message)
        {
            if (context.Response.HasStarted) return;

            context.Response.StatusCode = (int)status;
            context.Response.ContentType = "application/json";

            var payload = new ErrorResponse(
                Status: (int)status,
                Error: error,
                Message: message,
                Timestamp: DateTime.UtcNow
                );

            await context.Response.WriteAsJsonAsync(payload);
        }
    }
}
