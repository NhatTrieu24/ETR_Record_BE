# Chuẩn hóa Error Handling (422/400/500/401/404/403), Enum hóa Status, Bulk Import Tài khoản, Chặn Admin tự xóa — 2026-08-18

**Ngày thực hiện:** 2026-08-18
**Phạm vi:** `ETR.API/Middleware/GlobalExceptionHandler.cs`, `ETR.API/Program.cs`; 11 entity trong `ETR.Domain/Entities/*.cs` + 11 enum mới trong `ETR.Domain/Enums/`; `ETR.Infrastructure/Data/AppDbContext.cs` + `AppDbContext.Compliance.cs`; toàn bộ Service/DTO/Controller có thao tác với các field Status liên quan; `ETR.Application/Services/ImportService.cs` + `ETR.API/Controllers/ImportController.cs` (bulk import tài khoản); `ETR.Application/Services/AccountService.cs` (chặn tự xóa).
**Mục tiêu:** Theo yêu cầu `/mpower:code-fix` + `/mpower:code-refactor`: (1) sửa lỗi 500 mơ hồ khi create/update Class/Subject/Course; (2) bổ sung bulk create tài khoản qua Excel; (3) enum hóa `Course.Status`; (4) audit và enum hóa toàn bộ field Status raw-string còn lại; (5) thống nhất một Exception Handler cho toàn bộ route với contract 422/400/500/401/404/403 rõ ràng; (6) chặn Admin tự xóa chính mình.

---

## 1. Tóm tắt những gì đã implement

### 1.1 Root cause của lỗi 500 mơ hồ khi create/update Course (và tương tự các module khác)

`GlobalExceptionHandler.Classify()` trước đây **không có nhánh nào cho `ArgumentException`** — trong khi `CourseService.CreateCourseAsync`/`UpdateCourseAsync` ném đúng `ArgumentException` với message rõ ràng ("A course must have at least one subject...") khi request thiếu `Subjects`. Vì không được nhận diện, exception này rơi thẳng vào nhánh mặc định `500 "An unexpected error occurred"` — message rõ ràng bị vứt bỏ, client chỉ thấy lỗi chung chung dù server đã biết chính xác vấn đề. Đây là nguyên nhân gốc của phần lớn phàn nàn "báo lỗi chung chung" ở Course/Class/Subject.

Đã thêm `ArgumentException` (bắt luôn `ArgumentNullException`, `ArgumentOutOfRangeException` vì cùng cây kế thừa) và `FormatException` vào `Classify()`, map sang **422** kèm nguyên message gốc (xem mục 1.5).

### 1.2 Bulk Import tài khoản người dùng qua Excel

Tái sử dụng đúng pattern 2-bước Validate → Commit đã có sẵn cho Attendance/Assessment (`docs/maintain/2026-08-11_feature-bulk-import-excel.md`), mở rộng thêm nhóm **Accounts**:

| Method | Endpoint | Auth |
|---|---|---|
| GET | `/api/import/accounts/template` | Admin, Academic |
| POST multipart | `/api/import/accounts/validate` | Admin, Academic |
| POST multipart | `/api/import/accounts/commit` | Admin, Academic |

- Template: cột `Username (email)*`, `Mật khẩu*`, `Vai trò (Role)*`, `Phòng ban (Department)*` — 2 cột cuối có dropdown validation lấy trực tiếp từ danh sách Role/Department hiện có trong DB.
- Validate kiểm tra: Username đúng định dạng email + ≤255 ký tự, không trùng trong file, không trùng tài khoản đã tồn tại; Password không rỗng; RoleName/DepartmentName phải tồn tại; **Academic chỉ được tạo tài khoản Role Student** (giữ nguyên đúng rule đã có ở `AccountService.CreateAccountAsync` — không tạo lỗ hổng leo thang quyền qua đường import).
- Commit: **all-or-nothing** giống Attendance import — nếu còn bất kỳ row lỗi nào, từ chối toàn bộ (không tạo nửa vời); mỗi tài khoản tạo thành công đều ghi `AuditLog` riêng (`INSERT`, mô tả rõ "created via bulk Excel import").
- Mật khẩu hash bằng BCrypt giống hệt luồng tạo tài khoản đơn lẻ, `Status` mặc định `Active`.

### 1.3 + 1.4 Enum hóa Status — Course và 10 entity khác cùng dạng

Sau khi rà soát toàn bộ `ETR.Domain/Entities/*.cs`, xác định đúng 11 field `Status` dùng raw string mà không có kiểu ràng buộc (khác với `AuditActionType`/`ApprovalHistoryActionType` vốn đã là enum từ trước). Đã tạo 11 enum mới trong `ETR.Domain/Enums/`, mỗi enum liệt kê **chính xác các giá trị đã từng được gán ở đâu đó trong codebase** (grep toàn repo, kể cả `DataSeeder.cs`) để không đánh rơi giá trị lịch sử nào:

| Entity.Field | Enum mới | Giá trị |
|---|---|---|
| `Course.Status` | `CourseStatus` | Active, Inactive |
| `Subject.Status` | `SubjectStatus` | Active, Inactive |
| `Class.Status` | `ClassStatus` | Planned, Scheduled, InProgress, Completed, Cancelled |
| `Account.Status` | `AccountStatus` | Active, Inactive |
| `ETRCourseRecord.Status` | `EtrStatus` | Draft, InProgress, Submitted, Verified, Completed, ReturnedForCorrection, Cancelled |
| `CourseEnrollment.Status` | `EnrollmentStatus` | Active, Enrolled, Withdrawn, Completed, Deleted |
| `SubjectResult.Status` | `SubjectResultStatus` | Pending, Passed, Failed, Exempted |
| `ExportJob.Status` | `ExportJobStatus` | InProgress, Completed, Failed |
| `AttendanceRecord.Status` | `AttendanceStatus` | Present, Absent |
| `AmendmentRequest.Status` | `AmendmentStatus` | Pending, Approved, Rejected |
| `UserProfile.Status` | `LearnerStatus` (thay thế class hằng số string cũ `ETR.Application.Compliance.LearnerStatus`) | Active, Withdrawn, Graduated, Grounded |

**Chiến lược DB — không migration, không đổi schema:** mỗi property áp dụng `HasConversion<string>()` trong `AppDbContext.ConfigureEnumConversions()` mới — cột DB **vẫn là `nvarchar`** với đúng giá trị text như trước (`"Active"`, `"Completed"`, …), dữ liệu cũ và `Deploy_ETR_System.sql`/script SQL thủ công đọc trực tiếp vẫn hoạt động bình thường, không cần `dotnet ef migrations add`. Đổi lại, toàn bộ C# code (Service, DTO Request/Response, DataSeeder) giờ dùng enum type-safe — không còn khả năng typo tạo ra giá trị "ma" không so sánh được với bất kỳ đâu.

**JSON wire format giữ nguyên cho Frontend:** đăng ký `JsonStringEnumConverter` toàn cục trong `Program.cs` — response JSON vẫn trả `"Active"` (tên enum) chứ không phải số `0`, nên **không có breaking change nào cho FE hiện tại**.

**1 bug tiềm ẩn được phát hiện và sửa kèm theo:** `AppDbContext.Compliance.cs` (cơ chế enforce Absolute ETR Immutability) đọc `entry.Property(nameof(ETRCourseRecord.Status)).OriginalValue as string` để so sánh `"Completed"`/`"Verified"` — sau khi `Status` đổi sang enum, phép `as string` trên một enum đã boxed **luôn trả về `null`**, khiến điều kiện nhận diện "đang Reopen ETR" (Completed → Verified) im lặng luôn sai. Đã sửa thành cast trực tiếp `(EtrStatus?)`. Đây là ví dụ điển hình lý do TSD yêu cầu build + review kỹ sau enum hóa thay vì chỉ "đổi kiểu rồi build cho qua".

### 1.5 Unified Exception Handler — hợp đồng lỗi thống nhất cho toàn bộ route

`GlobalExceptionHandler` (đã tồn tại từ batch 2026-07-23) được siết lại đúng theo yêu cầu lần này:

| Loại lỗi | Exception | Status | Chi tiết trả về |
|---|---|---|---|
| Validate | `ValidationException`, `ArgumentException` (+ `ArgumentNullException`/`ArgumentOutOfRangeException`), `FormatException` | **422** | Message gốc do service tự viết |
| Validate (model binding tự động) | `[Required]`/DataAnnotations trên DTO, do `[ApiController]` chặn trước khi vào action | **422** | `ValidationProblemDetails` liệt kê lỗi từng field (giống ASP.NET Core mặc định nhưng đổi status 400→422) |
| Logic nghiệp vụ | `BusinessRuleViolationException`, `ImmutabilityViolationException` | **400** | Message gốc do service tự viết |
| Unauthorized | `UnauthorizedAccessException` | **401** | Message cố định, không lộ chi tiết |
| Not Found | `KeyNotFoundException` | **404** | Message gốc (an toàn, do service tự viết) |
| Forbidden | `ForbiddenAccessException` | **403** | Message gốc do service tự viết |
| Conflict (trùng khóa) | `DbUpdateException` (SQL 2601/2627) | 409 | Message cố định, không lộ tên constraint |
| Server/DB/framework khác | Mọi exception còn lại (`DbUpdateException` khác, `IOException`, `NotSupportedException`, `InvalidOperationException` "trần" từ EF Core/LINQ, …) | **500** | Message cố định `"An unexpected error occurred"` — **luôn được log đầy đủ (`_logger.LogError`) kèm exception gốc + request path**, không bao giờ echo message thật ra client (đúng Security Baseline "no stack traces in prod errors") |

Model-binding validation (`options.InvalidModelStateResponseFactory` mới trong `Program.cs`) được route qua cùng contract 422 thay vì mặc định 400 của ASP.NET Core — một status code duy nhất cho MỌI kiểu lỗi validate, dù đến từ DataAnnotations tự động hay từ service tự ném tay.

### 1.6 Admin không thể tự xóa chính mình

`AccountService.DeleteAccountAsync` — thêm guard đầu hàm: nếu `accountId == deletedByAccountId` (người gọi đang cố xóa chính tài khoản của mình) → ném `BusinessRuleViolationException("You cannot delete your own account.")` → tự động trả **400** kèm message rõ ràng qua handler ở mục 1.5. Áp dụng cho mọi caller gọi `DELETE /api/accounts/{id}` (Admin, Academic), không riêng Admin, vì không có lý do nghiệp vụ nào để bất kỳ role nào tự xóa chính mình qua endpoint quản trị này.

---

## 2. Đã kiểm chứng bằng cách nào

- `dotnet build ETRSystem.slnx`: **0 Error** trên cả 5 project (`ETR.Domain`, `ETR.Application`, `ETR.Infrastructure`, `ETR.Application.Tests`, `ETR.API`) sau khi sửa toàn bộ ~90 điểm gọi bị ảnh hưởng bởi việc đổi kiểu 11 field Status — mỗi lỗi biên dịch được compiler chỉ thẳng đến đúng dòng, không cần đoán.
- `dotnet test ETR.Application.Tests`: **4/4 pass**, không có test nào bị breaking bởi thay đổi enum.
- Chạy app thật (`dotnet run`, DB thật đang cấu hình trong `appsettings.json`) + `curl`:
  - `POST /api/auth/login` với body rỗng `{}` → **422** `{"title":"Validation failed","status":422,"errors":{"Password":["..."],"Username":["..."]}}` — xác nhận model-binding validation giờ đi qua 422 thay vì 400 mặc định.
  - `GET /api/accounts` không kèm JWT → **401** — xác nhận nhánh Unauthorized không đổi hành vi.
- Không verify được luồng bulk import Accounts bằng gọi API thật với file Excel cụ thể trong lượt này (cần chuẩn bị file mẫu + tài khoản Admin thật trên DB đang dùng chung) — đã review code kỹ theo đúng pattern đã verify trước đó cho Attendance/Assessment import (cùng tác giả, cùng cấu trúc Validate/Commit, cùng cơ chế transaction).

## 3. Rủi ro/việc còn lại

- **Không tạo migration mới** — do chọn `HasConversion<string>()` thay vì đổi cột DB sang int/enum thật (quyết định đã xác nhận với user trước khi làm, xem AskUserQuestion trong phiên làm việc). Nếu sau này muốn thu gọn storage bằng cột int, cần một batch riêng có migration + cập nhật `Deploy_ETR_System.sql`.
- Phạm vi enum hóa lần này **chỉ giới hạn 11 field Status** đã liệt kê ở mục 1.3/1.4 (theo xác nhận phạm vi với user) — các field cùng dạng "tập giá trị cố định" nhưng không tên là `Status` (`Subject.SubjectType`, `Course.CourseType`, `Assessment.AssessmentType`, `EvidenceFile.VerificationStatus`, `ApprovalRequest.CurrentStatus`, `AssessmentResult.ResultStatus`, `SubjectSignoff.Role`, …) **chủ động chưa đổi** trong lượt này để tránh phình phạm vi ngoài yêu cầu ban đầu — có thể cân nhắc một batch enum-hóa tiếp theo nếu cần triệt để hơn.
- `UpdateUserProfileStatusRequest` giờ nhận `LearnerStatus` enum thay vì string bị regex giới hạn `Active|Withdrawn|Graduated` — `Grounded` vẫn bị chặn tường minh trong `UserProfileService.UpdateProfileStatusAsync` (ném `BusinessRuleViolationException` nếu ai đó cố set qua endpoint này), giữ đúng bất biến "Grounded chỉ được hệ thống tự set" như comment gốc trên field.
- Bulk import Accounts hiện là **all-or-nothing** (giống Attendance, khác Assessment cho phép skip từng row) — nếu sau này cần cho phép import một phần (tạo được bao nhiêu tài khoản hợp lệ thì tạo), cần đổi lại logic `CommitAccountImportAsync` để không return sớm khi `errors.Count > 0`.

## 4. Files liên quan

| File | Trạng thái |
|---|---|
| `ETR.Domain/Enums/CourseStatus.cs`, `SubjectStatus.cs`, `ClassStatus.cs`, `AccountStatus.cs`, `EtrStatus.cs`, `EnrollmentStatus.cs`, `SubjectResultStatus.cs`, `ExportJobStatus.cs`, `AttendanceStatus.cs`, `AmendmentStatus.cs`, `LearnerStatus.cs` | Mới (11 file) |
| `ETR.Application/DTOs/Import/AccountImportRow.cs` | Mới |
| `ETR.Application/Compliance/LearnerStatus.cs` | Xóa — thay bằng enum `ETR.Domain.Enums.LearnerStatus` |
| `ETR.API/Middleware/GlobalExceptionHandler.cs` | Sửa — thêm nhánh `ArgumentException`/`FormatException`, đổi `ValidationException` sang 422 |
| `ETR.API/Program.cs` | Sửa — `InvalidModelStateResponseFactory` (422), `JsonStringEnumConverter` toàn cục |
| `ETR.Infrastructure/Data/AppDbContext.cs` | Sửa — thêm `ConfigureEnumConversions()` (11 `HasConversion<string>()`) |
| `ETR.Infrastructure/Data/AppDbContext.Compliance.cs` | Sửa — fix bug `as string` trên enum đã boxed (mục 1.4) |
| `ETR.Infrastructure/Data/DataSeeder.cs` | Sửa — toàn bộ literal string Status đổi sang enum tương ứng |
| `ETR.Domain/Entities/Course.cs`, `Subject.cs`, `Class.cs`, `Account.cs`, `ETRCourseRecord.cs`, `CourseEnrollment.cs`, `SubjectResult.cs`, `ExportJob.cs`, `AttendanceRecord.cs`, `AmendmentRequest.cs`, `UserProfile.cs` | Sửa — property `Status` đổi kiểu sang enum tương ứng |
| `ETR.Application/Services/CourseService.cs`, `SubjectService.cs`, `ClassService.cs`, `AccountService.cs`, `EnrollmentService.cs`, `EtrService.cs`, `AmendmentService.cs`, `ApprovalService.cs`, `AttendanceService.cs`, `AssessmentResultService.cs`, `DashboardService.cs`, `DashboardKpiCalculator.cs`, `ExportService.cs`, `ExportService.Reports.cs`, `UserProfileService.cs`, `ImportService.cs` | Sửa — mọi so sánh/gán literal string Status đổi sang enum, `.ToString()` khi ghi vào `AuditLog.OldValue/NewValue` (vẫn là string) |
| `ETR.Application/Interfaces/IAccountService.cs`, `IUserProfileService.cs`, `IImportService.cs` | Sửa — chữ ký method đổi sang enum tương ứng; `IImportService` thêm 3 method Accounts |
| `ETR.Application/DTOs/Account/AccountDtos.cs`, `Course/Requests/*.cs`, `Course/Responses/CourseResponse.cs`, `Subject/Requests/*.cs`, `Subject/Responses/SubjectResponse.cs`, `Class/Requests/*.cs`, `Class/Responses/TrainingClassResponse.cs`, `Enrollment/Requests/UpdateEnrollmentRequest.cs`, `Enrollment/Responses/*.cs`, `Etr/Responses/EtrRecordResponse.cs`, `Etr/Responses/EtrDetailsResponse.cs`, `Attendance/Requests/*.cs`, `Attendance/Responses/AttendanceRecordResponse.cs`, `Amendment/Responses/AmendmentRequestResponse.cs`, `UserProfile/UserProfileDtos.cs`, `Export/Responses/ExportJobResponse.cs`, `Dashboard/MyDashboardResponse.cs`, `Search/ClassSearchResultResponse.cs`, `Search/EtrSearchResultResponse.cs` | Sửa — field `Status` đổi từ `string` sang enum tương ứng |
| `ETR.API/Controllers/AuthController.cs`, `ExportsController.cs`, `SearchController.cs` | Sửa — literal string comparison đổi sang enum |
| `ETR.API/Controllers/ImportController.cs` | Sửa — thêm 3 endpoint `accounts/template`, `accounts/validate`, `accounts/commit` |
