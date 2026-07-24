# Siết chặt Error Handling: DomainException, DbUpdateException, File I/O, QuestPDF — loại bỏ 500 mơ hồ — 2026-07-23

**Ngày thực hiện:** 2026-07-23
**Phạm vi:** `GlobalExceptionHandler.cs`, mới: `BusinessRuleViolationException.cs`, `ForbiddenAccessException.cs`, 7 service (`ApprovalService`, `AssessmentResultService`, `AttendanceService`, `EnrollmentService`, `EtrService`, `PracticalChecklistResultService`, `ExportService`), `EvidenceService.cs`, `AuthController.cs`, `Program.cs`.
**Mục tiêu:** Sau `/mpower:flow-review-code` (báo cáo: `.claude/workspace/reviews/flow-review-2026-07-23-repo.md`), `/mpower:code-fix` các finding về exception-handling — mục tiêu người dùng đặt ra: hạn chế tối đa việc server trả thẳng lỗi 500 mơ hồ, đảm bảo lỗi trả về client rõ ràng nhất, đồng thời quản lý chặt hệ thống bắt lỗi của repo.

---

## 1. Tóm tắt những gì đã implement

### 1.1 Vấn đề gốc: `GlobalExceptionHandler` chỉ map 4 loại exception

Trước khi sửa, handler chỉ nhận diện `KeyNotFoundException` (404), `UnauthorizedAccessException` (401), `ValidationException` (400), `InvalidOperationException` (400) — mọi exception khác (kể cả `DbUpdateException` do trùng khoá/vi phạm FK, `IOException` khi ghi file, exception từ QuestPDF) đều rơi vào nhánh 500 "An unexpected error occurred" không có thông tin gì thêm.

Nghiêm trọng hơn: `InvalidOperationException` — loại exception .NET/EF Core cũng tự ném ra nội bộ (VD: `.Single()` rỗng, EF Core tracked-entity trùng khoá, `Enum.Parse` lỗi) — bị gán nhãn "Business rule violation" 400 giống hệt các lỗi nghiệp vụ do service tự ném, khiến bug thật bị hiểu nhầm thành lỗi input của user, và bị log ở mức `Warning` thay vì `Error` (không hiện trong dashboard giám sát lỗi thật).

### 1.2 Tách riêng `BusinessRuleViolationException` khỏi `InvalidOperationException` framework

Thêm `ETR.Application/Compliance/BusinessRuleViolationException.cs` — loại exception **duy nhất** mà handler echo `.Message` về client dưới mã 400 "Business rule violation". Đổi toàn bộ **43** vị trí `throw new InvalidOperationException(...)` (lỗi nghiệp vụ, message đã được viết an toàn từ trước) trên 7 service sang `BusinessRuleViolationException`. Từ nay, `InvalidOperationException` "trần" (do framework/EF Core/LINQ ném) sẽ rơi đúng vào 500 và log ở mức `Error` — không còn bị hiểu nhầm thành lỗi 400 của user.

### 1.3 Tách `ForbiddenAccessException` (403) khỏi `UnauthorizedAccessException` (401)

Grep toàn repo xác nhận: mọi `UnauthorizedAccessException` khác (ở tầng Controller, dạng `_currentUserService.AccountId ?? throw ...`) đều đúng nghĩa 401 (chưa đăng nhập/token không hợp lệ) — chỉ có 2 nơi ở tầng Service (`AttendanceService.cs`, `AssessmentResultService.cs`) ném exception này cho case "đã đăng nhập nhưng không có quyền xem dữ liệu học viên khác" — đúng ra phải là 403, không phải 401. Thêm `ForbiddenAccessException` (mới) map sang 403, đổi 2 nơi này sang dùng nó. Giữ nguyên `UnauthorizedAccessException → 401` cho đúng case còn lại.

### 1.4 `DbUpdateException` chưa từng được bắt ở đâu trong repo

Grep `DbUpdateException` trên toàn bộ `ETR.*` trả về 0 kết quả trước khi sửa — mọi lỗi trùng khoá duy nhất (`CourseCode`, `DepartmentName`, `SubjectCode`, `ClassCode`, `Username`, `Email`, v.v.) hoặc vi phạm khoá ngoại (RoleId/DepartmentId/EvidenceTypeId không tồn tại) đều rơi vào 500 mơ hồ. Đã thêm 1 nhánh xử lý tập trung trong `GlobalExceptionHandler`, dựa vào `SqlException.Number` bên trong `DbUpdateException`:
- `2601`/`2627` (trùng chỉ mục duy nhất) → **409 Conflict**, message chung chung (không lộ tên constraint).
- `547` (vi phạm khoá ngoại) → **400 Invalid reference**, message chung chung.
- Các `DbUpdateException` khác → vẫn 500 (an toàn, không đoán bừa nguyên nhân).

### 1.5 File I/O và QuestPDF không có try/catch

`EvidenceService.UploadEvidenceAsync` (ghi file evidence) và `ExportService.ExportTrainingPackageAsync` (ghi ZIP/PDF) trước đây không có try/catch quanh thao tác đĩa — `IOException` (đầy đĩa, file bị khoá) hoặc `UnauthorizedAccessException` (từ `System.IO`, **cùng loại** exception dùng cho lỗi xác thực, khiến lỗi quyền ghi file bị handler map nhầm thành 401 "Not authenticated") đều rơi vào 500 hoặc sai mã lỗi. Đã bọc try/catch ở cả 2 nơi, dịch sang `BusinessRuleViolationException` với message rõ ràng theo đúng ngữ cảnh (upload thất bại / export thất bại), đồng thời xoá file ZIP dở dang (0 byte) nếu ghi thất bại giữa chừng thay vì để lại rác public trong `wwwroot/uploads/exports`.

`ExportService.BuildPdf` (gọi QuestPDF `GeneratePdf()`) cũng được bọc try/catch riêng cho `DocumentDrawingException`/`DocumentComposeException`/`DocumentLayoutException` — một comment phê duyệt hoặc tên file evidence quá dài (tràn layout PDF) giờ trả về lỗi rõ ràng thay vì crash request với 500.

### 1.6 Hardening nhỏ

- `AuthController.Login`: `role!` (null-forgiving, có thể NRE nếu Role bị xoá mềm/mất đồng bộ) đổi thành `?? throw new BusinessRuleViolationException(...)` — không còn khả năng NullReferenceException rơi vào 500 khi generate token.
- `NotSupportedException` (từ `AppDbContext.SaveChanges()` đồng bộ — đã chặn từ batch P3) nay có nhánh xử lý tường minh trong handler thay vì rơi vào nhánh mặc định không tên.
- `Program.cs`: catch khởi động (`catch (Exception ex)` bọc toàn bộ `builder`/`app` setup) trước đây in cả `ex.Message` và `ex.StackTrace` ra stdout — có thể lộ fragment connection string nếu lỗi đến từ `SqlClient`/config binder. Nay chỉ log `ex.GetType().FullName` ra `Console.Error`, exception đầy đủ vẫn được `throw;` lại cho ai chạy interactive.

---

## 2. Đã kiểm chứng bằng cách nào

- `dotnet build` toàn solution sau mỗi batch sửa: **0 Error** (2 warning `NU1510` có sẵn từ trước, không liên quan).
- Chạy full app thật (`dotnet run`, SQL Server Docker cục bộ — `appsettings.json` hiện trỏ sang Azure `etr-db.database.windows.net` nên override tạm bằng biến môi trường `ConnectionStrings__DefaultConnection` chỉ cho phiên test này, không sửa file config) + `curl`, đăng nhập `admin@etr.com`:
  - `POST /api/etr/1/submit` (ETR đã khoá) → `400 {"title":"Business rule violation","detail":"ETR is locked."}` — xác nhận `BusinessRuleViolationException` vẫn map và echo message đúng như cũ.
  - `POST /api/courses` với `courseCode` trùng (`AMT-101`) → **409** `{"title":"Conflict","detail":"A record with the same unique value already exists."}` — trước đây là 500 mơ hồ; không có tên constraint bị lộ trong response; không có dòng rác nào được ghi (transaction rollback đúng).
  - `POST /api/accounts` với `roleId: 99999` (FK không tồn tại) → **400** `{"title":"Invalid reference","detail":"One or more referenced resources do not exist."}` — trước đây là 500; không có dòng rác được ghi.
  - `POST /api/exports/training-package` (ETR đã Completed) → vẫn **200**, tạo `ExportJob` thật — xác nhận try/catch mới bọc quanh QuestPDF + ZIP I/O không gây regression trên happy path.
- Không verify được `ForbiddenAccessException` (403) bằng gọi API thật trong lượt này — dữ liệu seed chỉ có đúng 1 `Enrollment`/`ClassStudent`, không có bản ghi của "học viên khác" để gọi chéo mà không phải tạo thêm account/enrollment mới (tốn thời gian hơn phạm vi 1 lượt smoke test) — đã verify bằng code review + build (đổi tên loại exception + thêm 1 nhánh handler, thay đổi cơ học, rủi ro thấp).
- `AuthController` role-null guard và thay đổi `Program.cs` verify bằng code review + build — cả 2 đòi hỏi điều kiện khó dựng lại an toàn trên DB dev dùng chung (xoá Role đang dùng / crash lúc khởi động).

## 3. Rủi ro/việc còn lại

- Báo cáo `flow-review-code` (`.claude/workspace/reviews/flow-review-2026-07-23-repo.md`) còn liệt kê các finding **chủ động không sửa trong lượt này** vì ngoài phạm vi "error-handling" người dùng yêu cầu — đã được user xác nhận qua gate G3 ("Approve --fix, error-handling scope only"):
  - **S0 (đã biết từ trước, đã bị hoãn ở batch P0)**: `GET /api/auth/mock-admin-token` cấp JWT admin không cần xác thực; SA password + JWT secret bị commit thẳng trong `appsettings.json`.
  - **S1**: `AuthController` có 4 endpoint mock (`GoogleLogin`, `RefreshToken`, `ChangePassword`, `ResetPassword`) trả về thành công giả mà không thực sự làm gì — `ChangePassword` nguy hiểm nhất (user tưởng đã đổi mật khẩu nhưng thực ra không).
  - **S2**: CORS đang cấu hình `AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()`; enumeration IDOR qua phân biệt 404 (không tồn tại) vs 401/403 (tồn tại nhưng không phải của mình) ở `AttendanceService`/`AssessmentResultService`.
  - **S3**: Rate limiting chỉ áp dụng cho `Login`/`ForgotPassword`/`ResetPassword`, chưa có giới hạn mặc định toàn cục.
- 43 message lỗi nghiệp vụ (`BusinessRuleViolationException`) vẫn còn một số chỗ lộ ID nội bộ dạng số (VD: `EtrService.cs` các message "Subject (ID: {cs.SubjectId})..."). Đã đánh giá: các endpoint này chỉ gọi được bởi Instructor/Admin/QA (đã bị role-gate), và ID hiển thị thuộc về chính học viên mà họ đang quản lý (không phải dữ liệu người khác ngoài phạm vi quản lý) — rủi ro thực tế thấp, và việc đổi ID số sang tên hiển thị (VD: `subject.SubjectName`) đòi hỏi thêm truy vấn/join mới ở nhiều nơi — **chủ động không làm** trong lượt này để tránh phình phạm vi ngoài yêu cầu "error handling" ban đầu; có thể cân nhắc riêng sau nếu cần.
- `appsettings.json` hiện trỏ `DefaultConnection` sang `etr-db.database.windows.net` (Azure) thay vì `localhost` như các lượt trước — thay đổi này không phải do lượt sửa lỗi này gây ra (phát hiện khi chạy smoke test, đã dùng biến môi trường override tạm để test, không đụng vào file config) — cần xác nhận với team đây là chủ đích chuyển môi trường hay cấu hình nhầm.

## 4. Files liên quan

| File | Trạng thái |
|---|---|
| `ETR.Application/Compliance/BusinessRuleViolationException.cs` | Mới |
| `ETR.Application/Compliance/ForbiddenAccessException.cs` | Mới |
| `ETR.API/Middleware/GlobalExceptionHandler.cs` | Sửa — thêm `DbUpdateException`/`IOException`/`NotSupportedException` arm, tách `BusinessRuleViolationException`/`ForbiddenAccessException`, ngừng echo `.Message` cho exception không rõ nguồn gốc |
| `ETR.Application/Services/ApprovalService.cs` | Sửa — đổi `InvalidOperationException` → `BusinessRuleViolationException` (5 chỗ) |
| `ETR.Application/Services/AssessmentResultService.cs` | Sửa — đổi `InvalidOperationException` → `BusinessRuleViolationException` (8 chỗ), `UnauthorizedAccessException` → `ForbiddenAccessException` (1 chỗ) |
| `ETR.Application/Services/AttendanceService.cs` | Sửa — đổi `InvalidOperationException` → `BusinessRuleViolationException` (2 chỗ), `UnauthorizedAccessException` → `ForbiddenAccessException` (1 chỗ) |
| `ETR.Application/Services/EnrollmentService.cs` | Sửa — đổi `InvalidOperationException` → `BusinessRuleViolationException` (4 chỗ) |
| `ETR.Application/Services/EtrService.cs` | Sửa — đổi `InvalidOperationException` → `BusinessRuleViolationException` (17 chỗ) |
| `ETR.Application/Services/PracticalChecklistResultService.cs` | Sửa — đổi `InvalidOperationException` → `BusinessRuleViolationException` (3 chỗ) |
| `ETR.Application/Services/ExportService.cs` | Sửa — đổi `InvalidOperationException` → `BusinessRuleViolationException` (4 chỗ), bọc try/catch cho `BuildPdf`/ZIP I/O |
| `ETR.Application/Services/EvidenceService.cs` | Sửa — bọc try/catch cho file upload |
| `ETR.API/Controllers/AuthController.cs` | Sửa — bỏ `role!` null-forgiving |
| `ETR.API/Program.cs` | Sửa — startup-crash catch không còn in `.Message`/`.StackTrace` ra stdout |
| `.claude/workspace/reviews/flow-review-2026-07-23-repo.md` | Mới — báo cáo review + remediation đầy đủ |
