namespace ETR.Application.Interfaces;

public interface IEmailTemplateRenderer
{
    string Render(string templateContent, IReadOnlyDictionary<string, string> tokens);

    string RenderTemplateFile(string templatesDirectory, string templateName, IReadOnlyDictionary<string, string> tokens);
}
