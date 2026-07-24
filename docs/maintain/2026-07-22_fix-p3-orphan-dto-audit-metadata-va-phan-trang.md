# Fix P3: Dọn DTO orphan, Gộp Signoff, Audit metadata (IP/UserAgent), Retake giữ lịch sử, Validation, Phân trang — 2026-07-22

**Ngày thực hiện:** 2026-07-22
**Phạm vi:** `AssessmentResultsController.cs`, `SubjectSignoffController.cs`, `AuditController.cs`, `RetakeHistoryController.cs`, `AppDbContext.cs`, `AppDbContext.Compliance.cs`, `ICurrentUserService.cs`, `CurrentUserService.cs`, `AssessmentResultService.cs`, 19 Request DTO, mới: `PagedResponse.cs`, 1 migration mới, xoá 27 DTO orphan.
**Mục tiêu:** Vá 8/8 mục P3 trong `ETR.Documentation/BAO_CAO_REVIEW_HE_THONG.md` (mục "🎯 Danh sách công việc ưu tiên" → "P3 × Rủi ro Thấp", mục 35-42) — tiếp nối các batch P0 (2026-07-20), P1 (2026-07-21), P2 (2026-07-21) đã xong.

---

## 1. Tóm tắt những gì đã implement

### 1.1 [Mục 35] Xoá DTO orphan còn lại

Báo cáo gốc chỉ nêu tên 2 ví dụ (`ClassInstructor`, Auth DTO cũ) nhưng grep toàn repo (không chỉ trong Controllers) cho thấy phạm vi thật lớn hơn nhiều: **27 file** DTO không còn được `using`/tham chiếu ở bất kỳ đâu, gồm 2 nhóm:
- Orphan thật (không còn action nào dùng): `ApprovalActionRequest`, `AssignInstructorRequest`, `BulkAttendanceItemRequest`/`BulkAttendanceRequest`, `ConfirmSessionRequest`, `Create/UpdateAttendanceSessionRequest`, `Create/UpdateChecklistItemRequest`, `Create/UpdateRoleRequest`, `Create/UpdateUserRequest`, `EtrActionRequest`, `EvidenceActionRequest`, `ClassInstructorResponse`, `LoginResponseDto`/`LoginRequest` (thay bằng `LoginRequestDto`), `CreateEtrRecordRequest`/`UpdateEtrRecordRequest` (orphan từ batch P0 xoá endpoint ETR ghi trực tiếp).
- Duplicate-tên-khác-namespace (2 bản cùng tên, chỉ 1 bản được controller `using` thật): `CompletionRequirement`, `EvidenceType`, `Department` (mỗi cặp Create/Update), và `Approval/CreateApprovalRequest.cs` (namespace con trùng tên với bản namespace phẳng đang dùng thật). Đã xác nhận bản nào "sống" bằng cách đọc đúng `using` của controller tiêu thụ trước khi xoá bản còn lại.

Xoá luôn 2 thư mục rỗng sau đó: `ETR.Application/DTOs/User/Requests`, `ETR.Application/DTOs/User`.
`dotnet build` sau xoá: **0 Error**.

### 1.2 [Mục 36] Gộp 2 endpoint Signoff trùng lặp

`AssessmentResultsController.SignoffSubject` (`POST /api/AssessmentResults/signoff`, role `Instructor,Admin`) và `SubjectSignoffController` (`POST /api/SubjectSignoff`, role `Instructor,Academic`) gọi cùng 1 hàm service (`SignoffSubjectResultAsync`) nhưng role khác nhau — rủi ro 1 bên được vá bảo mật còn bên kia thì không.
Đã gộp về **1 route duy nhất**: xoá hẳn action trong `AssessmentResultsController`, giữ `SubjectSignoffController` làm route chính thức, mở rộng role thành hợp của cả 2 (`Instructor,Academic,Admin`) để không ai bị mất quyền đang có.

### 1.3 [Mục 37] Sửa composite-key `RecordId=0` trong AuditLog cho CourseSubject

`CourseSubject` là entity duy nhất trong toàn model dùng composite key (`CourseId`+`SubjectId`) — xác nhận bằng grep toàn bộ `HasKey`. `AuditLog.RecordId` là 1 cột `int` nên không thể encode lossless cả 2 phần khoá; trước đây luôn ghi cứng `0`, mất khả năng truy vết.
Đã sửa `GetPrimaryKeyValue`: lấy giá trị của **property khoá đầu tiên** làm `RecordId` (ít nhất là 1 ID thật, truy vết được) — toàn bộ composite key vẫn có đầy đủ trong `OldValue`/`NewValue` JSON nên không mất thông tin.

### 1.4 [Mục 38] Bỏ sync-over-async `SaveChanges().GetAwaiter().GetResult()`

Grep `\.SaveChanges()` toàn repo xác nhận **không có nơi nào gọi** bản đồng bộ. Thay vì giữ pattern block-over-async (nguy cơ deadlock) hoặc âm thầm rơi về `base.SaveChanges()` (bỏ qua enforcement immutability/audit), đã override `SaveChanges()` để `throw NotSupportedException` rõ ràng, buộc mọi caller trong tương lai phải dùng `SaveChangesAsync`.

### 1.5 [Mục 39] Retake tạo dòng `AssessmentResult` mới thay vì ghi đè

Trước: `RecordAssessmentScoreAsync` mutate `existingResult` tại chỗ khi retake — mất điểm lần thi trước.
Đã sửa: mỗi lần chấm (kể cả retake) `INSERT` 1 dòng `AssessmentResult` mới với `AttemptNo` tăng dần, giữ nguyên lịch sử tất cả các lần thi. Unique index mở rộng thêm `AttemptNo` (migration `AddAttemptNoToAssessmentResultUniqueIndex`) — vẫn chặn 2 dòng cùng khai cùng 1 attempt number, nhưng cho phép nhiều `AttemptNo` khác nhau cùng tồn tại.

### 1.6 [Mục 40] Thêm `[Required]`/`[MaxLength]`/`[EmailAddress]` cho Request DTO

Rà 19 Request DTO còn thiếu validation cơ bản (Auth, Course, Class, Subject, Attendance, Assessment, Enrollment, Etr Return, SubjectSignoff) — thêm `[Required]`, `[MaxLength(N)]`, `[MinLength(N)]`, `[EmailAddress]` theo đúng ràng buộc nghiệp vụ của từng field (VD: `CourseCode` tối đa 20 ký tự, `Email` bắt buộc + đúng định dạng, mật khẩu mới tối thiểu 6 ký tự).
**Lưu ý kỹ thuật quan trọng phát hiện qua test runtime thật**: vì DTO là C# `record` với positional constructor, attribute phải đặt **không có** target `property:` (mặc định nhắm vào constructor parameter) — bản build đầu tiên dùng `[property: Required]` biên dịch thành công nhưng **runtime trả lỗi 400 "Business rule violation"** kèm message ASP.NET Core tự nói rõ: validation metadata trên property "sẽ bị bỏ qua", phải gắn vào constructor parameter. Đã sửa lại toàn bộ 19 file về đúng cú pháp (`[Required]` trần, không `property:`).

### 1.7 [Mục 41] Bổ sung capture IP/UserAgent vào AuditLog

`ICurrentUserService` thêm 2 property `IPAddress`/`UserAgent`; `CurrentUserService` (API layer) implement qua `IHttpContextAccessor` (`Connection.RemoteIpAddress`, header `User-Agent`).
`AppDbContext` nhận thêm `ICurrentUserService?` qua constructor (optional, không phá test/design-time context không có HTTP context); `SaveChangesAsync` gọi `StampAuditRequestMetadata()` để gán `IPAddress`/`UserAgent` vào mọi `AuditLog` mới tạo trong cùng lần save, trước khi ghi xuống DB.

### 1.8 [Mục 42] Thêm phân trang cho AuditController/RetakeHistoryController

2 endpoint list (`GET /api/audit`, `GET /api/retakehistory`) trước trả toàn bộ bảng không giới hạn. Thêm DTO chung `PagedResponse<T>` (`Items`/`TotalCount`/`Page`/`PageSize`), query param `page`/`pageSize` (mặc định 1/20), helper `NormalizePaging` chuẩn hoá (`page` tối thiểu 1, `pageSize` kẹp trong khoảng 1-100) — áp dụng cho cả `GetAuditLogs`, `SearchAuditLogs`, và `RetakeHistoryController.GetAll`.

---

## 2. Đã kiểm chứng bằng cách nào

- `dotnet build` toàn solution: **0 Error** (chỉ còn 2 warning `CS8604`/`NU1510` có sẵn từ trước, không liên quan tới batch này).
- Migration mới `AddAttemptNoToAssessmentResultUniqueIndex` tạo bằng `dotnet ef migrations add`, additive (drop + recreate 1 index, không đụng cột/dữ liệu) — áp dụng sạch lên DB dev thật.
- Chạy full app thật (`dotnet run`, SQL Server Docker) + `curl`:
  - `POST /api/auth/forgot-password` với `{"email":""}` → **400** với message validation chuẩn (`"The Email field is required."`, `"...is not a valid e-mail address."`) — xác nhận đã sửa đúng bug `[property: ...]`, không còn lỗi "metadata will be ignored".
  - `POST /api/AssessmentResults/signoff` (route cũ đã gộp) → **405** (không phải 404) — xác nhận là hành vi routing chuẩn của ASP.NET Core (URL trùng template `api/AssessmentResults/{id}` của action `GetById` còn lại, khác verb → 405), **không phải** route cũ còn sống sót; action Signoff thật sự đã bị xoá khỏi controller.
  - `POST /api/SubjectSignoff` (route gộp) → **401** (đúng, không có token hợp lệ).
  - `GET /api/audit?page=1&pageSize=5` và `GET /api/retakehistory?page=1&pageSize=5` → trả đúng cấu trúc `{items, totalCount, page, pageSize}`.
  - Tạo mới 1 Department qua API thật với header `User-Agent` tuỳ chỉnh → dòng `AuditLog` mới nhất có `ipAddress: "::1"`, `userAgent` đúng giá trị gửi lên — xác nhận capture IP/UserAgent hoạt động thật (dữ liệu audit cũ trước batch này vẫn `null` như kỳ vọng, vì tính năng chưa tồn tại khi ghi). Đã xoá lại Department test ngay sau khi verify.
  - Query trực tiếp SQL Server (`sys.indexes`) xác nhận `IX_AssessmentResults_AssessmentId_AccountId_SessionId_AttemptNo` (unique, filter `[SessionId] IS NOT NULL`) đã tồn tại thật trên DB, khớp migration.

## 3. Rủi ro/việc còn lại

- **Mục 39** mới verify bằng migration + schema thật trên DB, **chưa** dựng được 1 kịch bản retake đầu-cuối qua API thật (cần setup state SubjectResult/Session ở nhiều bước, tốn thời gian hơn phạm vi 1 lượt smoke test) — logic đã qua code review kỹ (đây là lần sửa thứ 3 trên cùng method sau các batch P2 mục 15/24).
- **Mục 40** chỉ phủ 19 DTO có field string cần validate rõ ràng; các DTO không có field string hoặc đã đủ DataAnnotations từ batch P2 không bị đụng tới (giữ đúng nguyên tắc surgical, không refactor ngoài phạm vi).
- **Mục 42** dùng phân trang in-memory (`Skip`/`Take` sau khi `GetAllAsync()`) — chưa đẩy xuống query SQL (`IQueryable`), nên với bảng `AuditLog`/`RetakeHistory` cực lớn vẫn tải toàn bộ bảng vào memory trước khi cắt trang; ngoài phạm vi mục 42 (báo cáo gốc chỉ yêu cầu "thêm phân trang", không yêu cầu tối ưu query).
- **27 DTO xoá ở mục 35** đã verify zero-reference bằng grep toàn repo, không chỉ Controllers — nhưng không loại trừ hoàn toàn khả năng có code Frontend/Postman collection ngoài repo này còn tham chiếu tên DTO cũ qua tài liệu API (Swagger) đã thay đổi.

## 4. Files liên quan

| File | Trạng thái |
|---|---|
| `ETR.Application/DTOs/**` (27 file orphan/duplicate) | Xoá |
| `ETR.Application/DTOs/User/Requests/`, `ETR.Application/DTOs/User/` | Xoá (thư mục rỗng) |
| `ETR.Application/DTOs/PagedResponse.cs` | Mới |
| `ETR.Infrastructure/Migrations/*_AddAttemptNoToAssessmentResultUniqueIndex.cs` | Mới |
| `ETR.API/Controllers/AssessmentResultsController.cs` | Sửa — xoá action `SignoffSubject` |
| `ETR.API/Controllers/SubjectSignoffController.cs` | Sửa — mở rộng role hợp nhất |
| `ETR.API/Controllers/AuditController.cs` | Sửa — phân trang `GetAuditLogs`/`SearchAuditLogs` |
| `ETR.API/Controllers/RetakeHistoryController.cs` | Sửa — phân trang `GetAll` |
| `ETR.Infrastructure/Data/AppDbContext.cs` | Sửa — nhận `ICurrentUserService?`, unique index thêm `AttemptNo` |
| `ETR.Infrastructure/Data/AppDbContext.Compliance.cs` | Sửa — `SaveChanges()` throw, `GetPrimaryKeyValue` composite key, `StampAuditRequestMetadata` |
| `ETR.Application/Interfaces/ICurrentUserService.cs` | Sửa — thêm `IPAddress`, `UserAgent` |
| `ETR.API/Services/CurrentUserService.cs` | Sửa — implement `IPAddress`, `UserAgent` |
| `ETR.Application/Services/AssessmentResultService.cs` | Sửa — retake tạo dòng mới thay vì ghi đè |
| 19 Request DTO (Auth, Course, Class, Subject, Attendance, Assessment, Enrollment, Etr Return, SubjectSignoff) | Sửa — thêm DataAnnotations |
