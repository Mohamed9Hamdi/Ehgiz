using Ehgiz.Application.Common;
using System.ComponentModel.DataAnnotations;

namespace Ehgiz.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        int statusCode;
        string message;

        switch (ex)
        {
            case KeyNotFoundException:
                statusCode = StatusCodes.Status404NotFound;
                message = ex.Message;
                break;
            case UnauthorizedAccessException:
                statusCode = StatusCodes.Status403Forbidden;
                message = ex.Message;
                break;
            case InvalidOperationException:
            case ValidationException:
                statusCode = StatusCodes.Status400BadRequest;
                message = ex.Message;
                break;
            default:
                _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
                statusCode = StatusCodes.Status500InternalServerError;
                message = "An unexpected error occurred.";
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(message));
    }
}
