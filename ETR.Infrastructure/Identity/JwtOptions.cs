namespace ETR.Infrastructure.Identity;

public class JwtOptions
{
    public const string SectionName = "JwtSettings";
    
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
}
