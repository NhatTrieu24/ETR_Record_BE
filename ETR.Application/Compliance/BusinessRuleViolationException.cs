namespace ETR.Application.Compliance;

// Client-safe domain-rule violation — the ONLY exception type GlobalExceptionHandler echoes
// .Message for as 400 "Business rule violation". Keeps framework-thrown InvalidOperationException
// (EF Core, LINQ, etc.) from being misclassified as a business rule and falling through to 500 instead.
public class BusinessRuleViolationException : Exception
{
    public BusinessRuleViolationException(string message) : base(message)
    {
    }
}
