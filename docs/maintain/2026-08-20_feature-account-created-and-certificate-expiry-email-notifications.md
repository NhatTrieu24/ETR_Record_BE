# Feature: Email thông báo tạo tài khoản & nhắc hết hạn chứng chỉ — 2026-08-20

**Ngày thực hiện:** 2026-08-20
**Phạm vi:** `ETR.Application/Services/AccountService.cs` (sửa); `ETR.Application/Compliance/CertificateValidityCalculator.cs` (sửa — thêm method mới), `CertificateExpiryCandidate.cs` (mới); `ETR.Application/Interfaces/ICertificateExpiryNotificationService.cs` (mới); `ETR.Application/Services/CertificateExpiryNotificationService.cs`, `CertificateExpiryScheduleCalculator.cs` (mới); `ETR.Application/DependencyInjection.cs` (sửa); `ETR.Infrastructure/Email/Templates/CertificateExpiryReminder.html` (mới); `ETR.API/BackgroundJobs/CertificateExpiryReminderJob.cs` (mới); `ETR.API/Program.cs` (sửa — đăng ký hosted service); test tương ứng trong `ETR.Application.Tests`.
**Phụ thuộc:** dựa trên module Email độc lập đã dựng ở `docs/maintain/2026-08-19_feature-email-module-gmail-smtp.md` — đây là lần đầu có business service thật sự gọi `IEmailService`.

---

## 1. Yêu cầu

1. Gửi email khi Admin (hoặc Academic) tạo account mới.
2. Gửi email nhắc học viên trước ngày hết hạn chứng chỉ ở 3 mốc: **3 ngày**, **7 ngày**, **30 ngày** (1 tháng).

## 2. Thiết kế

### 2.1 Email khi tạo account

`CreateAccountRequest.Username` có `[EmailAddress]` validation — tức **Username chính là email đăng nhập**, không có field email riêng ở bước tạo account (UserProfile với `FullName`/`Email` được tạo ở luồng khác, sau đó). Vì vậy:

- Gửi mail tới `account.Username`, dùng chính username làm `FullName` fallback trong template (chưa có UserProfile ở thời điểm này nên không có tên thật).
- Token `TemporaryPassword` = mật khẩu Admin vừa nhập ở `CreateAccountRequest.Password` — **đây là mật khẩu thật, gửi qua email ở dạng plaintext**, không phải mật khẩu tạm sinh ngẫu nhiên. Đây là hạn chế đã biết (xem mục 5).
- Gửi mail **sau khi** `SaveAsync` thành công, bọc trong `try/catch` riêng — lỗi SMTP (Gmail down, sai App Password,...) **không được** làm fail hoặc rollback việc tạo account. Log lỗi qua `ILogger<AccountService>` rồi tiếp tục trả `AccountResponse` bình thường.

### 2.2 Nhắc hết hạn chứng chỉ

**Không thêm cột DB mới** (tránh đụng migration đang có thay đổi song song ở nhánh này — `Attachment`, `SubjectResult` đang được sửa). Thay vào đó, dùng cơ chế **so khớp đúng ngày** — không lưu trạng thái "đã gửi":

- `CertificateValidityCalculator.GetCertificatesNearingExpiryAsync(unitOfWork, thresholdDays, nowUtc, ct)` (thêm vào file compliance đã có sẵn `HasAnyExpiredCompletedEtrAsync`, cùng logic "lấy ETRCourseRecord mới nhất theo Course/Enrollment" để không nhắc nhở dựa trên bản ghi đã bị thay thế bởi lần cấp chứng chỉ mới).
- Với mỗi (AccountId, CourseId), chỉ xét bản ghi `EtrStatus.Completed` mới nhất; nếu `(ExpiryDate.Date - Today.Date).Days` khớp đúng 3, 7, hoặc 30 → đưa vào danh sách cần nhắc.
- `CertificateExpiryNotificationService.SendExpiryRemindersAsync()` — với mỗi candidate, tra `UserProfile.Email`/`FullName`, `Course.CourseName`, gửi template `CertificateExpiryReminder.html`. Bỏ qua (log warning) nếu không có email; catch riêng lỗi gửi từng người để 1 email lỗi không chặn cả batch; trả về số email gửi thành công.
- `CertificateExpiryReminderJob : BackgroundService` (ETR.API) — chạy hằng ngày vào **06:00 UTC** (không phải "mỗi 24h kể từ lúc app start"), để tránh gửi trùng khi Azure Web App redeploy giữa ngày. Phần tính delay tới lần chạy kế tiếp tách ra `CertificateExpiryScheduleCalculator` (pure, có unit test) để test được mà không cần dựng hosted-service harness.

### 2.3 Vì sao không cần cờ "đã gửi" lưu DB

`daysUntilExpiry == threshold` chỉ đúng vào **đúng 1 ngày lịch** cho mỗi threshold, với điều kiện job chạy đúng 1 lần/ngày. Ưu điểm: không cần migration, không đụng entity. Đánh đổi: nếu job không chạy đúng ngày đó (crash, downtime), mốc nhắc đó bị **mất vĩnh viễn** cho record đó — không tự động gửi bù ngày sau. Xem mục 5.

---

## 3. Cách hoạt động end-to-end

**Tạo account:**
```
POST /api/accounts (Admin/Academic)
  → AccountService.CreateAccountAsync
    → tạo Account, SaveAsync
    → gửi email "AccountCreated.html" tới account.Username (best-effort, không rollback nếu lỗi)
```

**Nhắc hết hạn chứng chỉ:** không có endpoint mới — chạy nền tự động trong tiến trình `ETR.API`, mỗi ngày 06:00 UTC, quét toàn bộ `ETRCourseRecord` đang `Completed` và gửi mail cho các bản ghi khớp đúng 3/7/30 ngày trước hạn.

---

## 4. Test đã viết (TDD)

| File | Test case |
|---|---|
| `CertificateValidityCalculatorTests.cs` | Khớp đúng threshold trả về candidate; không khớp → rỗng; loại bản ghi chưa `Completed`; chọn đúng bản ghi mới nhất khi có nhiều bản ghi cùng course |
| `CertificateExpiryNotificationServiceTests.cs` | Gửi đúng token/đúng người khi có candidate khớp; bỏ qua khi thiếu email; trả 0 khi không có candidate; tiếp tục người sau khi 1 email lỗi |
| `CertificateExpiryScheduleCalculatorTests.cs` | Delay trong ngày khi chưa tới giờ chạy; delay sang hôm sau khi đã qua giờ chạy; delay = 24h khi đúng giờ chạy |
| `AccountServiceTests.cs` | Gửi email đúng tới username mới tạo; account vẫn được tạo & trả về bình thường dù gửi email thất bại |

Kết quả: `dotnet test ETR.Application.Tests` — **21/21 pass** (13 test mới, không regression trên các test đã có).

---

## 5. Giới hạn đã biết / Chưa làm

- **Không có cơ chế bù nhắc nhở nếu job miss 1 ngày** (app downtime đúng lúc 06:00 UTC, ví dụ) — record đó sẽ không được nhắc ở mốc đó nữa cho tới mốc kế tiếp (7 ngày → không tự nhảy về nhắc lại). Nếu cần độ tin cậy cao hơn, cần thêm cột lưu "đã gửi mốc nào" trên `ETRCourseRecord` (yêu cầu migration — ngoài scope lần này để tránh đụng migration đang chỉnh song song).
- **`CertificateExpiryReminderJob` (hosted-service glue) và schedule 06:00 UTC chưa test tích hợp thật** — chỉ test được phần tính delay thuần (`CertificateExpiryScheduleCalculator`). Dự án chưa có test project cho `ETR.API`.
- **Email tạo account gửi mật khẩu thật ở dạng plaintext** (không phải mật khẩu tạm sinh ngẫu nhiên) — vì `CreateAccountRequest.Password` do Admin tự đặt. Nếu cần chuẩn bảo mật cao hơn, nên đổi luồng thành "sinh mật khẩu tạm + bắt đổi mật khẩu lần đầu" (ngoài scope yêu cầu lần này).
- **FullName ở email tạo account = Username** (vì UserProfile chưa tồn tại ở bước tạo Account) — nếu sau này luồng tạo account được đổi để tạo kèm UserProfile, nên cập nhật lại token này cho có tên thật.
- **`AppPassword`/`SenderEmail` hiện đang nằm plaintext trong `ETR.API/appsettings.json` (đã bị commit-track)** — không phải do thay đổi trong phạm vi tài liệu này, nhưng cần cảnh báo: nếu file này được `git push`, App Password Gmail sẽ lộ trong lịch sử repo. Khuyến nghị chuyển sang `dotnet user-secrets` (local) / biến môi trường `EmailSettings__AppPassword` (server) như đã ghi trong doc module Email, và **thu hồi (revoke) App Password hiện tại trên Google rồi tạo App Password mới** nếu file này đã từng được push lên remote.
- **Gmail giới hạn ~500 email/ngày** cho tài khoản cá nhân qua SMTP — nếu số học viên gần hết hạn chứng chỉ trong 1 ngày vượt ngưỡng này, cần đổi provider (SendGrid/SES) hoặc thêm hàng đợi.

---

## 6. Files liên quan

| File | Trạng thái |
|---|---|
| `ETR.Application/Services/AccountService.cs` | Sửa — inject `IEmailService`, `ILogger`; gửi mail sau khi tạo account |
| `ETR.Application/Compliance/CertificateValidityCalculator.cs` | Sửa — thêm `GetCertificatesNearingExpiryAsync` |
| `ETR.Application/Compliance/CertificateExpiryCandidate.cs` | Mới |
| `ETR.Application/Interfaces/ICertificateExpiryNotificationService.cs` | Mới |
| `ETR.Application/Services/CertificateExpiryNotificationService.cs` | Mới |
| `ETR.Application/Services/CertificateExpiryScheduleCalculator.cs` | Mới |
| `ETR.Application/DependencyInjection.cs` | Sửa — đăng ký `ICertificateExpiryNotificationService` |
| `ETR.Infrastructure/Email/Templates/CertificateExpiryReminder.html` | Mới |
| `ETR.API/BackgroundJobs/CertificateExpiryReminderJob.cs` | Mới |
| `ETR.API/Program.cs` | Sửa — `AddHostedService<CertificateExpiryReminderJob>()` |
| `ETR.Application.Tests/Compliance/CertificateValidityCalculatorTests.cs` | Mới |
| `ETR.Application.Tests/Services/CertificateExpiryNotificationServiceTests.cs` | Mới |
| `ETR.Application.Tests/Services/CertificateExpiryScheduleCalculatorTests.cs` | Mới |
| `ETR.Application.Tests/Services/AccountServiceTests.cs` | Mới |
