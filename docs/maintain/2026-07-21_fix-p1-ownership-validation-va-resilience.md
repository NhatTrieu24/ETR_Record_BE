# Fix P1: Ownership Check, Validation & Resilience (Exception Handler + Rate Limiting) — 2026-07-21

**Ngày thực hiện:** 2026-07-21
**Phạm vi:** `AttendanceService.cs`, `AssessmentResultService.cs`, `EvidenceService.cs`, `EtrService.cs`, `EnrollmentService.cs`, `AuthController.cs`, `Program.cs`, `CreateSubjectSignoffRequest.cs`
**Mục tiêu:** Vá 10 mục P1 trong `ETR.Documentation/BAO_CAO_REVIEW_HE_THONG.md` (mục "Danh sách công việc ưu tiên" → "P1 × Rủi ro Cao/Trung bình — ngay sau P0", mục 11-20) — tiếp nối trực tiếp batch fix P0 đã xong (xem `docs/maintain/2026-07-20_fix-p0-authorization-va-etr-bypass.md`).

---

## 1. Tóm tắt những gì đã implement

### 1.1 [Mục 11] Ownership check cho Attendance/Assessment (chặn IDOR)

Trước đây `AttendanceService.GetAttendanceByClassStudentAsync` và `AssessmentResultService.GetAssessmentResultsByClassStudentAsync` nhận `accountId` (người gọi) làm tham số nhưng **không hề dùng nó để validate** — chỉ có comment "Zero-Trust validation could be enforced here" / "For simplicity, we just fetch results". Bất kỳ Student nào cũng xem được điểm/điểm danh của bất kỳ `classStudentId` nào khác.

**Đã sửa:** Thêm tham số `roleName` (lấy từ `ICurrentUserService.RoleName`) vào cả 2 method (interface + implementation + controller). Nếu `roleName == "Student"` và `classStudent.AccountId != accountId` (người gọi không phải chính học viên đó) → throw `UnauthorizedAccessException`. Instructor/QA/Admin không bị giới hạn thêm (giữ nguyên hành vi cũ).

File: `IAttendanceService.cs`, `AttendanceService.cs`, `AttendanceController.cs`, `IAssessmentResultService.cs`, `AssessmentResultService.cs`, `AssessmentResultsController.cs`.

### 1.2 [Mục 12] `SubjectSignoff.Role` lấy từ JWT, không còn client-supplied

Trước đây `CreateSubjectSignoffRequest` có field `Role` do client tự gửi (`request.Role`), lưu thẳng vào `SubjectSignoff.Role` — Student hoàn toàn có thể tự ký với `Role="Instructor"` để giả mạo vai trò xác nhận.

**Đã sửa:**
- Xoá field `Role` khỏi `CreateSubjectSignoffRequest` (chỉ còn `SubjectResultId`, `Comment`).
- `IAssessmentResultService.SignoffSubjectResultAsync` nhận thêm tham số `signoffByRoleName` — cả 2 controller gọi nó (`AssessmentResultsController.SignoffSubject`, `SubjectSignoffController.SignoffSubjectResult`) đều lấy giá trị này từ `_currentUserService.RoleName` (JWT claim), không còn tin giá trị từ body request.

### 1.3 [Mục 13] `EvaluateSubjectPassabilityAsync` check evidence phải `Verified`, không chỉ `Count>0`

Điều kiện #3 trong hàm này (mandatory evidence) trước đây chỉ check `evidenceFiles.Count == 0` — một evidence đã bị QA Reject vẫn khiến Subject Pass được. Đã sửa thành `evidenceFiles.Count == 0 || evidenceFiles.Any(e => e.VerificationStatus != "Verified")`, đúng pattern đã dùng trong `EtrService.SubmitEtrAsync`.

### 1.4 [Mục 14] Evidence: giới hạn MIME/size khi upload; ràng buộc `VerificationStatus` vào enum; bắt buộc comment khi Reject

`EvidenceService.UploadEvidenceAsync` trước đây nhận **bất kỳ file nào** không giới hạn size/type — nguy cơ stored-XSS nếu ai đó upload `.html`/`.svg` (evidence được serve tĩnh từ `wwwroot`). `VerifyEvidenceAsync` trước đây nhận `VerificationStatus` tuỳ ý từ client — gõ sai chính tả (VD `"verified"` viết thường) sẽ khiến evidence kẹt vĩnh viễn ở trạng thái không xác định vì Submit-gate check đúng chuỗi `"Verified"`.

**Đã sửa:**
- `UploadEvidenceAsync`: validate trước khi ghi file — allowlist extension (`.jpg .jpeg .png .gif .webp .pdf`), allowlist MIME type tương ứng, cap size **10 MB**. Vi phạm bất kỳ điều kiện nào → `ValidationException` (400), không ghi file xuống đĩa.
- `VerifyEvidenceAsync`: `VerificationStatus` phải là đúng `"Verified"` hoặc `"Rejected"` (so sánh `Ordinal`, phân biệt hoa/thường) — sai → `ValidationException`. Nếu `VerificationStatus == "Rejected"` mà `VerificationComment` rỗng/null/whitespace → `ValidationException` bắt buộc phải có comment.

### 1.5 [Mục 15] `RecordAssessmentScoreAsync` verify `AccountId` khớp học viên thật của khoá học

Trước đây `request.AccountId` (client-supplied) được dùng thẳng để tạo/ghi đè `AssessmentResult` mà không xác minh gì — Instructor A về lý thuyết có thể "chấm điểm hộ" cho một `AccountId` bất kỳ, kể cả người chưa từng ghi danh vào khoá học chứa assessment đó.

**Đã sửa:** Sau khi load `Assessment`, tra tất cả `ClassStudent` có `AccountId == request.AccountId`, lấy danh sách `ClassId`, rồi kiểm tra có `Class` nào trong danh sách đó có `CourseId == assessment.CourseId` không. Không có → `InvalidOperationException` ("Account (ID: X) is not enrolled in a class for this assessment's course."), transaction rollback.

### 1.6 [Mục 16] `EtrService.ReturnEtrAsync` đổi status thành `"ReturnedForCorrection"`

Trước: `etr.Status = "Draft"` (sai tên so với tài liệu nghiệp vụ, và khác với `ApprovalService`'s Return branch vốn set đúng `"ReturnedForCorrection"` — nhưng chỉ trên `ApprovalRequest`, không phải `ETRCourseRecord`). Đã sửa `EtrService.ReturnEtrAsync` dùng đúng `"ReturnedForCorrection"` cho cả `AuditLog.NewValue` và `etr.Status`.

**Không mở rộng phạm vi:** không wiring `ApprovalService`'s Return/Reject/Verify branches gọi qua `EtrService` (đó là finding riêng "Reject/Return 2 luồng không đồng bộ" trong báo cáo, vẫn `still_open` — ngoài phạm vi mục 16, chỉ yêu cầu đồng bộ *tên chuỗi trạng thái*, không yêu cầu hợp nhất luồng).

### 1.7 [Mục 17] `DeleteEnrollmentAsync` chặn xoá nếu ETR đã tiến triển/khoá

Trước đây method này soft-delete `CourseEnrollment` vô điều kiện — xoá enrollment dù ETR đã `Submitted`/`Verified`/`Completed` hoặc `IsLocked`, để lại `ETRCourseRecord` mồ côi (không còn `CourseEnrollment` gốc). Đã thêm guard: tra `ETRCourseRecord` theo `EnrollmentId`, nếu tồn tại và (`IsLocked == true` HOẶC `Status` khác `"Draft"`/`"InProgress"`) → `InvalidOperationException`, không cho xoá.

### 1.8 [Mục 18] Global exception handler (`IExceptionHandler` + `ProblemDetails`)

Trước đây không có xử lý exception tập trung — mọi `throw` từ service (KeyNotFoundException, UnauthorizedAccessException, InvalidOperationException, ValidationException...) rơi thẳng xuống handler mặc định của ASP.NET Core, có nguy cơ lộ stack trace ở môi trường Development và trả sai HTTP status code.

**Đã thêm:** File mới `ETR.API/Middleware/GlobalExceptionHandler.cs` implement `IExceptionHandler` (pattern chuẩn từ .NET 8+), map:
- `KeyNotFoundException` → 404
- `UnauthorizedAccessException` → 401 (khớp convention hiện tại: controller ném exception này cho case "chưa đăng nhập")
- `ValidationException` (DataAnnotations, dùng trong `EvidenceService`) → 400
- `InvalidOperationException` (bao gồm cả `ImmutabilityViolationException` vì nó kế thừa từ class này) → 400
- Mọi exception khác → 500, message chung chung cho client, **log đầy đủ exception thật ở server** qua `ILogger`

Đăng ký trong `Program.cs`: `AddExceptionHandler<GlobalExceptionHandler>()` + `AddProblemDetails()`, và `app.UseExceptionHandler()` là middleware đầu tiên trong pipeline (trước Swagger/CORS/Auth) để bắt được exception từ toàn bộ pipeline phía sau.

### 1.9 [Mục 19] Rate limiting cho `/login`, `/forgot-password`, `/reset-password`

Dùng `Microsoft.AspNetCore.RateLimiting` có sẵn trong framework (.NET 7+, không cần thêm package). Policy `"AuthPolicy"`: fixed-window, **5 request/phút**, partition theo `RemoteIpAddress` (không phải giới hạn chung toàn hệ thống — mỗi IP có quota riêng), vượt quá → `429 Too Many Requests`. Áp dụng qua `[EnableRateLimiting("AuthPolicy")]` trên đúng 3 action (`Login`, `ForgotPassword`, `ResetPassword`), không áp cho toàn `AuthController`.

### 1.10 [Mục 20] `SubjectResult.CreatedByAccountId` dùng actor thay vì learner

Trong `EnrollmentService.CreateEnrollmentAsync`, đoạn tạo `CourseEnrollment`/`ETRCourseRecord` đã đúng dùng `createdByAccountId` (actor thực hiện ghi danh, VD Admin), nhưng đoạn tạo `SubjectResult` ngay bên dưới (trong vòng `foreach` qua `courseSubjects`) lại dùng nhầm `accountId` (chính học viên) cho `CreatedByAccountId` — sai audit trail. Đã sửa thành `createdByAccountId`, nhất quán với 2 entity còn lại được tạo trong cùng method.

---

## 2. Đã kiểm chứng bằng cách nào

- `dotnet build` toàn solution: **0 Error**, cùng 4 warning có sẵn từ trước (không liên quan tới các thay đổi này).
- Chạy full app thật (`dotnet run`, SQL Server Docker) 3 lần với `curl` để verify runtime thật (không chỉ code review):
  1. Login với tài khoản seed thật (`admin@etr.com` / `123456`, mật khẩu đã hash BCrypt từ batch P0) → **200**, JWT hợp lệ trả về — xác nhận batch P0 vẫn hoạt động đúng sau khi thêm middleware mới.
  2. `GET /api/etr/999999` (ETR không tồn tại) với JWT hợp lệ → **404**, body `{"title":"Resource not found","status":404,"detail":"ETRCourseRecord not found.","instance":"/api/etr/999999"}` — xác nhận `GlobalExceptionHandler` hoạt động đúng, không lộ stack trace.
  3. Gọi `POST /api/auth/login` với mật khẩu sai liên tục 7 lần trong 1 phút → 2 lần đầu **401**, 3 lần sau **429** — xác nhận rate limiting hoạt động đúng ngưỡng 5 req/phút/IP.
  4. `GET /api/etr/{id}` với JWT giả (invalid signature) → **401** (từ JWT Bearer middleware, không qua `GlobalExceptionHandler` — đúng, vì đây không phải exception ném từ code mà là auth challenge chuẩn của framework).
- Không có test project trong repo — không chạy được unit/integration test tự động cho các nhánh logic (VD: ownership check IDOR, MIME allowlist reject case) — chỉ verify qua code review + build + smoke test runtime như trên.

## 3. Rủi ro/việc còn lại (ngoài phạm vi lần fix này)

- **"Reject/Return 2 luồng không đồng bộ"** (S1, đã ghi nhận trong báo cáo): `ApprovalService`'s Reject/Verify/Return branches vẫn chỉ set `ApprovalRequest.CurrentStatus`, không đồng bộ `ETRCourseRecord` — mục 16 chỉ sửa đúng chuỗi status trong `EtrService.ReturnEtrAsync`, không hợp nhất 2 luồng.
- **"Login full-table scan + non-constant-time"** (S1): rate limiting (mục 19) giảm rủi ro brute-force nhưng KHÔNG sửa gốc — `AuthController.Login` vẫn load toàn bộ bảng `Accounts` (`GetAllAsync`) rồi filter bằng LINQ thay vì query có điều kiện `WHERE Username = ...` — vẫn `still_open`, ngoài phạm vi P1 batch này (không có trong 10 mục được yêu cầu).
- **10 MB size cap và allowlist MIME cho Evidence** là giá trị mặc định hợp lý tự chọn (báo cáo gốc không chỉ định con số cụ thể) — cần xác nhận lại với business nếu cần giới hạn khác.
- Global exception handler map `InvalidOperationException` → 400 (không phải 409 Conflict) — lựa chọn đơn giản hoá vì phần lớn message hiện tại có dạng "Cannot X because Y", phù hợp semantics 400 hơn; có thể cân nhắc 409 cho các case rõ ràng là state-conflict nếu muốn REST-strict hơn.

## 4. Files liên quan

| File | Trạng thái |
|---|---|
| `ETR.API/Middleware/GlobalExceptionHandler.cs` | Mới |
| `ETR.API/Program.cs` | Sửa — đăng ký exception handler + rate limiter, thêm middleware vào pipeline |
| `ETR.API/Controllers/AuthController.cs` | Sửa — `[EnableRateLimiting("AuthPolicy")]` trên 3 action |
| `ETR.API/Controllers/AttendanceController.cs` | Sửa — truyền `roleName` cho ownership check |
| `ETR.API/Controllers/AssessmentResultsController.cs` | Sửa — truyền `roleName` cho ownership check + signoff |
| `ETR.API/Controllers/SubjectSignoffController.cs` | Sửa — truyền `roleName` cho signoff |
| `ETR.Application/Interfaces/IAttendanceService.cs` | Sửa — thêm tham số `roleName` |
| `ETR.Application/Interfaces/IAssessmentResultService.cs` | Sửa — thêm tham số `roleName`/`signoffByRoleName` |
| `ETR.Application/Services/AttendanceService.cs` | Sửa — ownership check |
| `ETR.Application/Services/AssessmentResultService.cs` | Sửa — ownership check, signoff role, evidence-verified check, score ownership check |
| `ETR.Application/Services/EvidenceService.cs` | Sửa — MIME/size validation, VerificationStatus enum + comment bắt buộc |
| `ETR.Application/Services/EtrService.cs` | Sửa — `ReturnEtrAsync` status string |
| `ETR.Application/Services/EnrollmentService.cs` | Sửa — guard `DeleteEnrollmentAsync`, fix `CreatedByAccountId` |
| `ETR.Application/DTOs/SubjectResult/Requests/CreateSubjectSignoffRequest.cs` | Sửa — xoá field `Role` |
