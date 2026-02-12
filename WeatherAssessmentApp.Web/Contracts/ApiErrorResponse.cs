namespace WeatherAssessmentApp.Web.Contracts;

public sealed record ApiErrorResponse(string Message, string? Detail, string TraceId);
