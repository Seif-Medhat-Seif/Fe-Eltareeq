using System.Net;
using System.Text.Json;
using RestaurantOrderingPlatform.Application.Exceptions;

namespace RestaurantOrderingPlatform.Api.Common.Middleware;

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
        catch (ConflictException ex)
        {
            await WriteErrorAsync(context, HttpStatusCode.Conflict, ex.Message);
        }
        catch (NotFoundException ex)
        {
            await WriteErrorAsync(context, HttpStatusCode.NotFound, ex.Message);
        }
        catch (UnauthorizedAppException ex)
        {
            await WriteErrorAsync(context, HttpStatusCode.Unauthorized, ex.Message);
        }
        catch (IdentityOperationException ex)
        {
            await WriteErrorAsync(context, HttpStatusCode.BadRequest, "Operation failed.", ex.Errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteErrorAsync(context, HttpStatusCode.InternalServerError, "An unexpected error occurred.");
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, HttpStatusCode statusCode, string message, IReadOnlyList<string>? errors = null)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var payload = new { statusCode = (int)statusCode, message, errors };
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
