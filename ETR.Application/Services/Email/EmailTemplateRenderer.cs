using System.Text.RegularExpressions;
using ETR.Application.Interfaces;

namespace ETR.Application.Services.Email;

public sealed partial class EmailTemplateRenderer : IEmailTemplateRenderer
{
    public string Render(string templateContent, IReadOnlyDictionary<string, string> tokens)
    {
        var missingTokens = new List<string>();

        var rendered = TokenPattern().Replace(templateContent, match =>
        {
            var key = match.Groups[1].Value;
            if (tokens.TryGetValue(key, out var value))
            {
                return value;
            }

            missingTokens.Add(key);
            return match.Value;
        });

        if (missingTokens.Count > 0)
        {
            throw new InvalidOperationException(
                $"Thiếu token khi render email template: {string.Join(", ", missingTokens)}");
        }

        return rendered;
    }

    public string RenderTemplateFile(string templatesDirectory, string templateName, IReadOnlyDictionary<string, string> tokens)
    {
        var templatePath = Path.Combine(templatesDirectory, templateName);
        if (!File.Exists(templatePath))
        {
            throw new InvalidOperationException(
                $"Không tìm thấy email template '{templateName}' tại '{templatesDirectory}'.");
        }

        var templateContent = File.ReadAllText(templatePath);
        return Render(templateContent, tokens);
    }

    [GeneratedRegex(@"\{\{(\w+)\}\}")]
    private static partial Regex TokenPattern();
}
