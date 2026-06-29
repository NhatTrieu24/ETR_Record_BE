namespace ETR.Application.Exceptions;

public class ImmutabilityViolationException : InvalidOperationException
{
    public ImmutabilityViolationException(string message)
        : base(message)
    {
    }

    public ImmutabilityViolationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
