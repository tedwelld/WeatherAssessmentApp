namespace WeatherAssessmentApp.Application.Exceptions;

public sealed class ExternalServiceException : AppException
{
    public ExternalServiceException(string message, int statusCode, bool isTransient = false) : base(message)
    {
        StatusCode = statusCode;
        IsTransient = isTransient;
    }

    public ExternalServiceException(string message, int statusCode, bool isTransient, Exception innerException) : base(message, innerException)
    {
        StatusCode = statusCode;
        IsTransient = isTransient;
    }

    public int StatusCode { get; }
    public bool IsTransient { get; }
}
