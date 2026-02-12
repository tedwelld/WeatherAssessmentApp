using System.Net;
using System.Text.Json;
using WeatherAssessmentApp.Application.Exceptions;
using WeatherAssessmentApp.Web.Contracts;

namespace WeatherAssessmentApp.Web.Middleware;

public sealed class GlobalExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
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
            var traceId = context.TraceIdentifier;
            var statusCode = ex switch
            {
                ValidationException => (int)HttpStatusCode.BadRequest,
                NotFoundException => (int)HttpStatusCode.NotFound,
                ConflictException => (int)HttpStatusCode.Conflict,
                ConcurrencyException => (int)HttpStatusCode.Conflict,
                ExternalServiceException externalServiceException => externalServiceException.StatusCode,
                _ => (int)HttpStatusCode.InternalServerError
            };

            if (statusCode >= 500)
            {
                _logger.LogError(ex, "Unhandled exception. TraceId: {TraceId}", traceId);
            }
            else
            {
             _logger.LogWarning("Request failed ({StatusCode}). TraceId: {TraceId}. Message: {Message}",
    statusCode, traceId, ex.Message);


            }

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var payload = new ApiErrorResponse(
                ex.Message,
                statusCode >= 500 ? "An unexpected error occurred." : null,
                traceId);

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
        }
    }
}
