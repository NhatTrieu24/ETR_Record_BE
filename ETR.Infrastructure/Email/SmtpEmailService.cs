using ETR.Application.DTOs.Email;
using ETR.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ETR.Infrastructure.Email;

public sealed class SmtpEmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly IEmailTemplateRenderer _templateRenderer;

    public SmtpEmailService(IOptions<EmailOptions> options, IEmailTemplateRenderer templateRenderer)
    {
        _options = options.Value;
        _templateRenderer = templateRenderer;
    }

    public async Task SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var mimeMessage = BuildMimeMessage(message);

        using var client = new SmtpClient
        {
            CheckCertificateRevocation = false
        };
        var secureSocketOptions = _options.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
        await client.ConnectAsync(_options.SmtpHost, _options.SmtpPort, secureSocketOptions, cancellationToken);
        await client.AuthenticateAsync(_options.SenderEmail, _options.AppPassword, cancellationToken);
        await client.SendAsync(mimeMessage, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    public async Task SendTemplatedEmailAsync(
        string toEmail,
        string toName,
        string templateName,
        string subject,
        IReadOnlyDictionary<string, string> tokens,
        CancellationToken cancellationToken = default)
    {
        var templatesRoot = Path.Combine(AppContext.BaseDirectory, _options.TemplatesDirectory);
        var htmlBody = _templateRenderer.RenderTemplateFile(templatesRoot, templateName, tokens);

        var message = new EmailMessage
        {
            ToEmail = toEmail,
            ToName = toName,
            Subject = subject,
            HtmlBody = htmlBody
        };

        await SendEmailAsync(message, cancellationToken);
    }

    private MimeMessage BuildMimeMessage(EmailMessage message)
    {
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(_options.SenderName, _options.SenderEmail));
        mimeMessage.To.Add(new MailboxAddress(message.ToName ?? message.ToEmail, message.ToEmail));
        mimeMessage.Subject = message.Subject;
        mimeMessage.Body = new BodyBuilder
        {
            HtmlBody = message.HtmlBody,
            TextBody = message.PlainTextBody
        }.ToMessageBody();

        return mimeMessage;
    }
}
