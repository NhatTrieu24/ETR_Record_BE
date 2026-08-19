using ETR.Application.Services.Email;

namespace ETR.Application.Tests.Services.Email;

public class EmailTemplateRendererTests
{
    private readonly EmailTemplateRenderer _renderer = new();

    [Fact]
    public void Render_ReplacesAllTokens_WhenAllTokensProvided()
    {
        var template = "<p>Xin chào {{FullName}}, tài khoản {{Username}} đã được tạo.</p>";
        var tokens = new Dictionary<string, string>
        {
            ["FullName"] = "Nguyễn Văn A",
            ["Username"] = "nguyenvana"
        };

        var result = _renderer.Render(template, tokens);

        Assert.Equal("<p>Xin chào Nguyễn Văn A, tài khoản nguyenvana đã được tạo.</p>", result);
    }

    [Fact]
    public void Render_ThrowsInvalidOperationException_WhenTokenMissing()
    {
        var template = "<p>Xin chào {{FullName}}, mã OTP của bạn là {{OtpCode}}.</p>";
        var tokens = new Dictionary<string, string> { ["FullName"] = "Nguyễn Văn A" };

        var ex = Assert.Throws<InvalidOperationException>(() => _renderer.Render(template, tokens));
        Assert.Contains("OtpCode", ex.Message);
    }

    [Fact]
    public void RenderTemplateFile_ThrowsInvalidOperationException_WhenFileMissing()
    {
        var templatesDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(templatesDirectory);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _renderer.RenderTemplateFile(templatesDirectory, "DoesNotExist.html", new Dictionary<string, string>()));

        Assert.Contains("DoesNotExist.html", ex.Message);
    }

    [Fact]
    public void RenderTemplateFile_ReturnsRenderedContent_WhenFileExists()
    {
        var templatesDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(templatesDirectory);
        var templatePath = Path.Combine(templatesDirectory, "Sample.html");
        File.WriteAllText(templatePath, "<p>Chào {{FullName}}</p>");

        var result = _renderer.RenderTemplateFile(templatesDirectory, "Sample.html",
            new Dictionary<string, string> { ["FullName"] = "Trâm" });

        Assert.Equal("<p>Chào Trâm</p>", result);
    }
}
