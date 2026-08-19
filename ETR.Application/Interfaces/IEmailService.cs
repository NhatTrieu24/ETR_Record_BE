using ETR.Application.DTOs.Email;

namespace ETR.Application.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default);

    Task SendTemplatedEmailAsync(
        string toEmail,
        string toName,
        string templateName,
        string subject,
        IReadOnlyDictionary<string, string> tokens,
        CancellationToken cancellationToken = default);
}
