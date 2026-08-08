# Fix: Chuẩn hóa ApprovalActionType & Xóa bỏ Entity trùng lặp ClassStudent — 2026-08-08

**Ngày thực hiện:** 2026-08-08
**Phạm vi:** `ETR.Application/DTOs/Approval/ApprovalActionType.cs` (mới); `ETR.Application/{Interfaces/IApprovalService,Services/ApprovalService}.cs`, `ETR.API/Controllers/ApprovalsController.cs` (mục #6); `ETR.Domain/Entities/{ClassStudent.cs (xóa), AttendanceRecord}.cs`, `ETR.Infrastructure/Data/{AppDbContext,AppDbContext.Compliance,DataSeeder}.cs`, `ETR.Infrastructure/Migrations/*_RemoveClassStudentPointToEnrollment.cs` (mới), `ETR.Application/{Interfaces,Services}/{IUnitOfWork,UnitOfWork,AttendanceService,IAttendanceService,AssessmentResultService,IAssessmentResultService,ExportService.Reports}.cs`, `ETR.API/Controllers/{AttendanceController,AssessmentResultsController,ClassStudentsController (xóa)}.cs`, 3 DTO (mục #10); test cập nhật `ETR.Application.Tests/Services/{ApprovalServiceTests,AttendanceServiceTests,EnrollmentServiceTests}.cs`.
**Mục tiêu:** `/mpower:code-fix` — xử lý mục #6 (Chuẩn hóa ActionType cho `ApprovalsController.process`) và mục #10 (Entity trùng lặp `CourseEnrollment` vs `ClassStudent`) trong `docs/todo/9.todo_to_complete_system.md`.

---

## Phần 1 — Mục #6: Chuẩn hóa `ApprovalActionType`

### Vấn đề trước khi sửa

`POST /api/approvals/{id}/process` nhận `action` dạng chuỗi tự do (`[FromQuery] string action`). Việc phân quyền theo từng action (`AllowedRolesByAction`) đã tồn tại đúng trong `ApprovalService` từ trước, nhưng vì `action` là `string`, giá trị sai chính tả hoặc không hợp lệ chỉ bị phát hiện SAU KHI vào tới Service (throw `BusinessRuleViolationException("Invalid action.")`), và FE không có cách nào biết trước 4 giá trị hợp lệ ngoài đọc code/tài liệu tay.

### Giải pháp

- Thêm `enum ApprovalActionType { Verify, Approve, Reject, Return }` (`ETR.Application/DTOs/Approval/`).
- Đổi `ProcessApproval` action trong `ApprovalsController` nhận `[FromQuery] ApprovalActionType action` thay vì `string` — ASP.NET Core model binding tự chặn giá trị ngoài 4 enum này bằng **400 Bad Request**, TRƯỚC KHI chạm tới `ApprovalService`. Swagger/OpenAPI cũng tự phát sinh đúng kiểu enum, FE generate client có type-safety thật thay vì string tự do.
- `ApprovalService.ProcessApprovalActionAsync` đổi tham số `action` sang `ApprovalActionType`, `AllowedRolesByAction` đổi key từ `string` sang enum — loại bỏ nhánh "unknown action" (`_ => throw ...`) khỏi luồng runtime vì không còn cách nào gọi tới với giá trị lạ.
- `ApprovalHistory.ActionType`/`AuditLog.ActionType` (cột `string` trong DB) vẫn lưu `action.ToString()` — không đổi schema, chỉ đổi phía type-safety ở tầng API/Service.

### Đã kiểm chứng

- Test cũ (`ApprovalServiceTests`) chuyển từ `InlineData(string, string)` sang `InlineData(ApprovalActionType, string)`.
- 1 test cũ (`ProcessApprovalActionAsync_WhenActionIsUnknown_ExpectsBusinessRuleViolationException`) đã **xóa** vì không còn khả năng gọi Service với action lạ — model binding chặn từ trước, không có gì để Service tự kiểm tra thêm.

---

## Phần 2 — Mục #10: Xóa bỏ Entity trùng lặp `ClassStudent`

### Vấn đề trước khi sửa

`ClassStudent` là bảng trung gian lặp dữ liệu 100% với `CourseEnrollment` (`AccountId`, `ClassId`, `Status` đều là bản sao). Mỗi lần Enroll, code phải tạo cả 2 bản ghi; mỗi lần sửa/withdraw Enrollment, code phải tự tay đồng bộ `ClassStudent` theo — 2 nguồn sự thật cho cùng 1 dữ liệu, không được DB enforce.

### Giải pháp

**Xóa hẳn `ClassStudent`**, đổi `AttendanceRecord.ClassStudentId` trỏ thẳng `CourseEnrollment.EnrollmentId`:

1. **Entity**: xóa `ClassStudent.cs`; `AttendanceRecord.ClassStudentId` → `AttendanceRecord.EnrollmentId`.
2. **`EnrollmentService`**: xóa toàn bộ 3 đoạn code tạo/đồng bộ/cascade `ClassStudent` (tạo lúc Enroll, sync lúc Update, cascade lúc Withdraw) — `CourseEnrollment` giờ là nguồn sự thật DUY NHẤT.
3. **`AttendanceService`**: mọi chỗ dùng `ClassStudentRepository`/`ClassStudentId` đổi sang `CourseEnrollmentRepository`/`EnrollmentId`; `GetAttendanceByClassStudentAsync` đổi tên thành `GetAttendanceByEnrollmentAsync(enrollmentId,...)`.
4. **`AssessmentResultService`**: `GetAssessmentResultsByClassStudentAsync` đổi tên thành `GetAssessmentResultsByEnrollmentAsync(enrollmentId,...)`, tra `enrollment.AccountId` thay vì qua `ClassStudent`.
5. **`ExportService.Reports.cs`**: báo cáo điểm danh Excel dùng `CourseEnrollment` thay vì `ClassStudent` để lookup học viên.
6. **`AppDbContext`/`AppDbContext.Compliance.cs`**: xóa `DbSet<ClassStudent>`, cấu hình khóa/FK/index liên quan; `ResolveEnrollmentIdAsync` (dùng cho kiểm tra bất biến dữ liệu — immutability check) giờ đọc thẳng `AttendanceRecord.EnrollmentId`, **không cần round-trip DB nữa** (trước đây phải query `ClassStudents` để tra `CourseEnrollmentId`).
7. **`DataSeeder.cs`**: bỏ seed `ClassStudent`; `GetDemoContextAsync` trả `CourseEnrollment` thay vì `ClassStudent`.
8. **Xóa hẳn** `ClassStudentsController.cs`, `ClassStudentResponse.cs` — không còn entity để expose API riêng.
9. **DTO đổi tên field**: `AttendanceRecordResponse.ClassStudentId` → `EnrollmentId`; `CreateAttendanceRecordRequest.ClassStudentId` → `EnrollmentId`.

### ⚠️ Breaking change cho FE — cần thông báo trước khi deploy

- `POST /api/attendance/record` — body đổi field `classStudentId` → `enrollmentId`.
- `GET /api/attendance/student/{classStudentId}` → đổi thành `GET /api/attendance/enrollment/{enrollmentId}`.
- `GET /api/assessmentresults/student/{classStudentId}` → đổi thành `GET /api/assessmentresults/enrollment/{enrollmentId}`.
- Toàn bộ `api/classstudents/*` (`GET /`, `GET /class/{classId}`, `GET /enrollment/{enrollmentId}`) **đã bị xóa** — FE cần thay bằng gọi trực tiếp `api/enrollments/*` nếu còn dùng.
- Mọi response có field `classStudentId` trước đây giờ trả `enrollmentId` — giá trị số cũng khác (không phải đổi tên field giữ nguyên giá trị) vì đây là 2 khóa chính khác nhau của 2 bảng khác nhau.

### Migration & backfill dữ liệu

`AddColumn`/`RenameColumn` của EF Core mặc định chỉ đổi TÊN cột, KHÔNG đổi GIÁ TRỊ — nếu chạy migration do EF tự sinh mà không sửa tay, cột `AttendanceRecords.EnrollmentId` (sau khi rename từ `ClassStudentId`) vẫn giữ nguyên các số `ClassStudentId` cũ, trỏ sai sang `CourseEnrollment` (2 bảng có 2 dải khóa chính độc lập). Migration `RemoveClassStudentPointToEnrollment` đã được sửa tay để chèn thêm 1 câu `UPDATE` bằng raw SQL **TRƯỚC** khi xóa bảng `ClassStudents` và đổi tên cột:

```sql
UPDATE ar
SET ar.ClassStudentId = cs.CourseEnrollmentId
FROM AttendanceRecords ar
INNER JOIN ClassStudents cs ON cs.ClassStudentId = ar.ClassStudentId;
```

Sau câu lệnh này, giá trị trong cột mới chính xác là `EnrollmentId` tương ứng trước khi cột bị đổi tên và bảng `ClassStudents` bị xóa. **`Down()` không phục hồi được dữ liệu gốc** (chỉ dựng lại đúng schema rỗng) — đã ghi rõ trong code comment, chấp nhận được vì đây là migration một chiều có chủ đích (loại bỏ hẳn 1 bảng dư thừa).

## Đã kiểm chứng bằng cách nào

- **TDD**: cập nhật toàn bộ test hiện có (`AttendanceServiceTests`, `EnrollmentServiceTests`, `ApprovalServiceTests`) từ `ClassStudent`/`string action` sang `CourseEnrollment`/`ApprovalActionType` → xác nhận RED (build lỗi do đổi signature/xóa entity) → sửa từng file → xác nhận GREEN.
- `dotnet build` toàn solution: **0 Error** (2 warning pre-existing, không liên quan).
- `dotnet ef migrations add RemoveClassStudentPointToEnrollment`: EF Core tự cảnh báo "operation was scaffolded that may result in the loss of data" — đúng như dự đoán, đã sửa tay thêm backfill SQL như trên.
- `dotnet test`: **65/65 pass** (66 trước đó − 1 test `ApprovalActionType` lạc hậu đã xóa = 65), không regression.
- Rà soát lại toàn bộ codebase (`grep -rn "ClassStudent"`) sau khi sửa — chỉ còn code comment giải thích lịch sử, không còn tham chiếu code thật nào tới entity đã xóa.

## Files liên quan

| File | Trạng thái |
|---|---|
| `ETR.Application/DTOs/Approval/ApprovalActionType.cs` | Mới |
| `ETR.Application/Interfaces/IApprovalService.cs`, `ETR.Application/Services/ApprovalService.cs` | Sửa — đổi `action` sang enum |
| `ETR.API/Controllers/ApprovalsController.cs` | Sửa — đổi query param sang enum |
| `ETR.Domain/Entities/ClassStudent.cs` | **Xóa** |
| `ETR.Domain/Entities/AttendanceRecord.cs` | Sửa — `ClassStudentId` → `EnrollmentId` |
| `ETR.Infrastructure/Data/AppDbContext.cs`, `AppDbContext.Compliance.cs`, `DataSeeder.cs` | Sửa |
| `ETR.Infrastructure/Migrations/20260808172355_RemoveClassStudentPointToEnrollment.cs` (+ `.Designer.cs`) | Mới — đã sửa tay thêm backfill SQL |
| `ETR.Application/Interfaces/IUnitOfWork.cs`, `ETR.Infrastructure/Repositories/UnitOfWork.cs` | Sửa — xóa `ClassStudentRepository` |
| `ETR.Application/Services/EnrollmentService.cs` | Sửa — xóa toàn bộ logic tạo/đồng bộ `ClassStudent` |
| `ETR.Application/Services/AttendanceService.cs`, `IAttendanceService.cs` | Sửa — đổi sang `CourseEnrollment`/`EnrollmentId` |
| `ETR.Application/Services/AssessmentResultService.cs`, `IAssessmentResultService.cs` | Sửa — đổi sang `CourseEnrollment`/`EnrollmentId` |
| `ETR.Application/Services/ExportService.Reports.cs` | Sửa — báo cáo điểm danh dùng `CourseEnrollment` |
| `ETR.API/Controllers/AttendanceController.cs`, `AssessmentResultsController.cs` | Sửa — đổi route/param |
| `ETR.API/Controllers/ClassStudentsController.cs` | **Xóa** |
| `ETR.Application/DTOs/ClassStudentResponse.cs` | **Xóa** |
| `ETR.Application/DTOs/Attendance/Responses/AttendanceRecordResponse.cs`, `Requests/CreateAttendanceRecordRequest.cs` | Sửa — đổi field |
| `ETR.Application.Tests/Services/{ApprovalServiceTests,AttendanceServiceTests,EnrollmentServiceTests}.cs` | Sửa |
| `docs/todo/9.todo_to_complete_system.md`, `ETR.Documentation/final/9.todo_to_complete_system.md` | Sửa — đánh dấu mục #6, #10 đã triển khai |
