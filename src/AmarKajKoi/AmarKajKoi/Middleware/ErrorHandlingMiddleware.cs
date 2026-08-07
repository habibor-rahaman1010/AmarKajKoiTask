using System.Net;
using System.Text.Json;

namespace AmarKajKoi.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext ctx)
        {
            try
            {
                await _next(ctx);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized");
                await WriteError(ctx, HttpStatusCode.Forbidden, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Bad request");
                await WriteError(ctx, HttpStatusCode.BadRequest, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled");
                await WriteError(ctx, HttpStatusCode.InternalServerError, "Server error.");
            }
        }

        private static Task WriteError(HttpContext ctx, HttpStatusCode code, string message)
        {
            ctx.Response.ContentType = "application/json";
            ctx.Response.StatusCode = (int)code;
            var payload = JsonSerializer.Serialize(new { message });
            return ctx.Response.WriteAsync(payload);
        }
    }
}
