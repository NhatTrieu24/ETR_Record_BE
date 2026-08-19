namespace ETR.Application.DTOs.Email;

public sealed class EmailMessage
{
    public required string ToEmail { get; init; }
    public string? ToName { get; init; }
    public required string Subject { get; init; }
    public required string HtmlBody { get; init; }
    public string? PlainTextBody { get; init; }
}
