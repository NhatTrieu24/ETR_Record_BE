# Feature: Email Module (Gmail SMTP) — 2026-08-19

**Ngày thực hiện:** 2026-08-19
**Phạm vi:** `ETR.Application/Interfaces/IEmailService.cs`, `IEmailTemplateRenderer.cs` (mới); `ETR.Application/DTOs/Email/EmailMessage.cs` (mới); `ETR.Application/Services/Email/EmailTemplateRenderer.cs` (mới); `ETR.Infrastructure/Email/*` (mới — `EmailOptions`, `SmtpEmailService`, `EmailServiceCollectionExtensions`, `Templates/AccountCreated.html`); `ETR.Infrastructure/DependencyInjection.cs` (sửa — gọi `AddEmailModule`); `ETR.Infrastructure/ETR.Infrastructure.csproj` (thêm package `MailKit`); `ETR.API/appsettings.json` (thêm section `EmailSettings`); `ETR.Application.Tests/Services/Email/EmailTemplateRendererTests.cs` (mới).
**Mục tiêu:** Dựng một module gửi email **độc lập**, không phụ thuộc/không đụng vào bất kỳ business service nào hiện có (`AccountService`, `EtrService`,...). Service nào cần gửi mail sau này chỉ cần constructor-inject `IEmailService` — không cần biết Gmail SMTP hay MailKit ở bên dưới.

---

## 1. Vấn đề trước khi làm

Hệ thống chưa có cơ chế gửi email nào. Các nhu cầu sắp tới (thông báo tạo tài khoản, reset mật khẩu, nhắc hạn ETR,...) đều cần một điểm gửi mail chung, dùng lại được, và **không được** viết logic SMTP rải rác trong từng service nghiệp vụ.

## 2. Thiết kế

### 2.1 Ranh giới module (Clean Architecture)

```
ETR.Application/Interfaces/
  IEmailService.cs            → contract gửi mail (implementation-agnostic)
  IEmailTemplateRenderer.cs   → contract render template (pure, không I/O phụ thuộc SMTP)
ETR.Application/DTOs/Email/
  EmailMessage.cs              → DTO thuần: ToEmail, ToName, Subject, HtmlBody, PlainTextBody
ETR.Application/Services/Email/
  EmailTemplateRenderer.cs     → implementation thuần string (regex token {{Key}}), KHÔNG phụ thuộc MailKit

ETR.Infrastructure/Email/
  EmailOptions.cs               → bind từ appsettings "EmailSettings" (theo đúng pattern JwtOptions/JwtSettings)
  SmtpEmailService.cs           → implementation IEmailService dùng MailKit, gửi qua Gmail SMTP
  EmailServiceCollectionExtensions.cs → AddEmailModule(IServiceCollection, IConfiguration)
  Templates/AccountCreated.html → template mẫu minh hoạ (không tự động gọi từ AccountService)
```

Bất kỳ project nào muốn gửi mail chỉ cần biết `IEmailService` (nằm ở Application, tầng mà mọi service khác đã tham chiếu) — không có reference ngược từ business logic vào MailKit hay Gmail. Tương tự pattern `ITokenService`/`TokenService` đã có (interface ở Application, implementation hạ tầng ở Infrastructure).

### 2.2 Vì sao MailKit thay vì `System.Net.Mail.SmtpClient`

`SmtpClient` (BCL) đã được Microsoft khuyến cáo ngừng dùng cho code mới — không hỗ trợ async đúng nghĩa, dễ treo connection pool. MailKit hỗ trợ `ConnectAsync`/`AuthenticateAsync`/`SendAsync` thật, TLS ổn định hơn với Gmail. Đã cập nhật lên **MailKit 4.17.0** (bản 4.9.0 mặc định của NuGet có CVE mức moderate — xem `GHSA-9j88-vvj5-vhgr`).

### 2.3 Template rendering

Không dùng engine template nặng (Scriban, RazorLight) — chỉ cần regex replace `{{TokenName}}` trong file `.html` tĩnh, vì nhu cầu hiện tại là email thông báo đơn giản (không cần loop/condition trong template). Nếu thiếu token, `EmailTemplateRenderer.Render` throw `InvalidOperationException` liệt kê tên token còn thiếu — fail-fast, không gửi mail với nội dung placeholder rác ra ngoài.

### 2.4 Cấu hình & secret

`EmailSettings` trong `appsettings.json` theo đúng pattern `JwtSettings`:

```json
"EmailSettings": {
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": 587,
  "SenderEmail": "",
  "SenderName": "ETR Management System",
  "AppPassword": "",
  "EnableSsl": true,
  "TemplatesDirectory": "Email/Templates"
}
```

`SenderEmail` và `AppPassword` **để trống trong repo** — không commit credential thật. Set giá trị thật qua:
- **Local dev:** `dotnet user-secrets set "EmailSettings:SenderEmail" "..."` và `dotnet user-secrets set "EmailSettings:AppPassword" "..."` trong `ETR.API`.
- **Server/CI:** biến môi trường `EmailSettings__SenderEmail` và `EmailSettings__AppPassword` (double underscore — ASP.NET Core config binding tự override theo convention chuẩn, không cần code thêm).

---

## 3. Cách dùng (cho service nghiệp vụ khác)

```csharp
public class SomeService
{
    private readonly IEmailService _emailService;

    public SomeService(IEmailService emailService) => _emailService = emailService;

    public async Task NotifyAccountCreatedAsync(string email, string fullName, string username, string tempPassword, CancellationToken ct)
    {
        await _emailService.SendTemplatedEmailAsync(
            toEmail: email,
            toName: fullName,
            templateName: "AccountCreated.html",
            subject: "Tài khoản ETR Management của bạn đã được tạo",
            tokens: new Dictionary<string, string>
            {
                ["FullName"] = fullName,
                ["Username"] = username,
                ["TemporaryPassword"] = tempPassword
            },
            cancellationToken: ct);
    }
}
```

Hoặc gửi mail không cần template: `_emailService.SendEmailAsync(new EmailMessage { ToEmail = ..., Subject = ..., HtmlBody = ... })`.

**Lưu ý quan trọng — lần này KHÔNG tích hợp:** module chỉ dựng interface + implementation, **chưa** có bất kỳ business service nào (`AccountService`,...) gọi `IEmailService`. Đây là quyết định có chủ đích của yêu cầu — team tự quyết khi nào và ở đâu wire vào (ví dụ sau khi tạo account, sau khi reset password, v.v.).

---

## 4. Cấu trúc file mới

| File | Loại |
|---|---|
| `ETR.Application/Interfaces/IEmailService.cs` | Interface |
| `ETR.Application/Interfaces/IEmailTemplateRenderer.cs` | Interface |
| `ETR.Application/DTOs/Email/EmailMessage.cs` | DTO |
| `ETR.Application/Services/Email/EmailTemplateRenderer.cs` | Service (pure, có unit test) |
| `ETR.Infrastructure/Email/EmailOptions.cs` | Options (bind config) |
| `ETR.Infrastructure/Email/SmtpEmailService.cs` | Implementation IEmailService (MailKit) |
| `ETR.Infrastructure/Email/EmailServiceCollectionExtensions.cs` | DI extension `AddEmailModule` |
| `ETR.Infrastructure/Email/Templates/AccountCreated.html` | Template mẫu |
| `ETR.Application.Tests/Services/Email/EmailTemplateRendererTests.cs` | Unit test |

---

## 5. Test đã viết (TDD)

Viết test cho `EmailTemplateRenderer` **trước** khi implement (TDD), chạy `dotnet test` xác nhận pass trước khi sang phần Infrastructure:

| Test | Mục tiêu |
|---|---|
| `Render_ReplacesAllTokens_WhenAllTokensProvided` | Token hợp lệ được thay đúng giá trị |
| `Render_ThrowsInvalidOperationException_WhenTokenMissing` | Fail-fast khi thiếu token, message chứa tên token thiếu |
| `RenderTemplateFile_ThrowsInvalidOperationException_WhenFileMissing` | Fail-fast khi không tìm thấy file template |
| `RenderTemplateFile_ReturnsRenderedContent_WhenFileExists` | Đọc file thật từ đĩa + render đúng |

Kết quả: `dotnet test ETR.Application.Tests` — **4/4 pass** (test mới), tổng test suite hiện có (**8 test**) không có regression.

---

## 6. Giới hạn đã biết / Chưa làm

- **`SmtpEmailService.BuildMimeMessage` (logic build `MimeMessage` từ MailKit) chưa có unit test** — dự án hiện chỉ có `ETR.Application.Tests` (không có `ETR.Infrastructure.Tests`), và phần này phụ thuộc trực tiếp kiểu MailKit/MimeKit nên không thể test thuần trong Application. Nếu cần coverage cho phần này, phải tạo mới một test project cho `ETR.Infrastructure` (ngoài scope lần này).
- **Chưa test gửi mail thật qua network** (theo đúng yêu cầu — không test I/O thật ra Gmail trong unit test). Cần verify thủ công bằng cách set `EmailSettings:SenderEmail`/`AppPassword` qua user-secrets rồi gọi `IEmailService` từ một endpoint tạm hoặc console test trước khi dùng ở production.
- **Chưa wire vào bất kỳ business flow nào** (đúng yêu cầu của lần này) — `AccountService` và các service khác vẫn chưa gọi `IEmailService`.
- **Không có retry/queue khi Gmail SMTP timeout hoặc rate-limit** — Gmail giới hạn ~500 email/ngày cho tài khoản cá nhân qua SMTP App Password. Nếu nhu cầu gửi mail tăng, cần đánh giá lại (SendGrid/SES) hoặc thêm hàng đợi (Hangfire/Azure Queue) — implementation hiện tại gửi đồng bộ, không có cơ chế retry.
- **Template hiện chỉ có 1 mẫu (`AccountCreated.html`)** — các use case khác (reset password, nhắc hạn ETR,...) cần tạo thêm file `.html` tương ứng trong `Email/Templates/`.

---

## 7. Files liên quan

| File | Trạng thái |
|---|---|
| `ETR.Application/Interfaces/IEmailService.cs` | Mới |
| `ETR.Application/Interfaces/IEmailTemplateRenderer.cs` | Mới |
| `ETR.Application/DTOs/Email/EmailMessage.cs` | Mới |
| `ETR.Application/Services/Email/EmailTemplateRenderer.cs` | Mới |
| `ETR.Infrastructure/Email/EmailOptions.cs` | Mới |
| `ETR.Infrastructure/Email/SmtpEmailService.cs` | Mới |
| `ETR.Infrastructure/Email/EmailServiceCollectionExtensions.cs` | Mới |
| `ETR.Infrastructure/Email/Templates/AccountCreated.html` | Mới |
| `ETR.Application.Tests/Services/Email/EmailTemplateRendererTests.cs` | Mới |
| `ETR.Infrastructure/DependencyInjection.cs` | Sửa — gọi `AddEmailModule(configuration)` |
| `ETR.Infrastructure/ETR.Infrastructure.csproj` | Sửa — thêm `PackageReference MailKit 4.17.0` + copy `Templates/**/*.html` ra output |
| `ETR.API/appsettings.json` | Sửa — thêm section `EmailSettings` (giá trị nhạy cảm để trống, set qua user-secrets/env var) |
