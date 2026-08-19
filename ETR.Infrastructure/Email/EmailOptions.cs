namespace ETR.Infrastructure.Email;

public class EmailOptions
{
    public const string SectionName = "EmailSettings";

    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string AppPassword { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    public string TemplatesDirectory { get; set; } = "Email/Templates";
}
