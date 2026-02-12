namespace WeatherAssessmentApp.Application.Exceptions;

public sealed class ConcurrencyException : AppException
{
    public ConcurrencyException(string message) : base(message)
    {
    }

    public ConcurrencyException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
