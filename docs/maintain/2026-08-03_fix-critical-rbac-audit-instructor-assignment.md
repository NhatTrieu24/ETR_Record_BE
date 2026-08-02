# Fix Critical C2→C8: RBAC segregation of duties, Reject workflow, Audit Trail, Instructor-Class assignment — 2026-08-03

**Ngày thực hiện:** 2026-08-03
**Phạm vi:** `ETR.API/Controllers/EvidencesController.cs`, `ApprovalsController.cs`, `EtrController.cs`, `AccountsController.cs`; `ETR.Application/Services/EvidenceService.cs`, `ApprovalService.cs`, `EtrService.cs`, `AccountService.cs`, `EnrollmentService.cs`, `CourseService.cs`, `ClassService.cs`; `ETR.Application/Interfaces/IApprovalService.cs`, `IAccountService.cs`, `IEtrService.cs`; `ETR.Domain/Entities/Class.cs`; `ETR.Infrastructure/Data/AppDbContext.cs` + 1 migration mới; `ETR.Application/DTOs/Class/*`; mới: `ETR.Application.Tests/Services/EvidenceServiceTests.cs`, `ApprovalServiceTests.cs`.
**Mục tiêu:** `/mpower:code-refactor` để hoàn thành 7 mục Critical (C2→C8) trong `ETR.Documentation/LO_TRINH_HOAN_THIEN_DU_AN.md` — các gap RBAC/audit/business-logic nghiêm trọng nhất được phát hiện qua đối chiếu FRD gốc với codebase hiện tại. C1 (đổi mật khẩu mock), C9 (secrets commit), C10 (mock-admin-token) chủ động **không nằm trong phạm vi lượt này** theo yêu cầu người dùng.

---

## 1. Tóm tắt những gì đã implement

### 1.1 [C2] Segregation of duties cho Evidence Verify

Trước: `[Authorize(Roles = "Instructor,QA,Admin")]` cho phép Instructor tự verify minh chứng do chính mình upload — phá vỡ nguyên tắc kiểm soát độc lập mà FRD dùng làm ví dụ minh hoạ RBAC.
Đã sửa: role Verify còn `QA,Admin`. Thêm guard trong `EvidenceService.VerifyEvidenceAsync`: nếu `evidence.UploadedByAccountId == verifiedByAccountId` → `ForbiddenAccessException("You cannot verify evidence that you uploaded yourself.")` — áp dụng cho MỌI role (kể cả Admin), không chỉ Instructor.

### 1.2 [C3 + C6] Viết lại `ApprovalService.ProcessApprovalActionAsync`

Đây là thay đổi lớn nhất — gộp chung vì cả 2 mục đều sửa cùng 1 method:
- **C3**: role-check chuyển hẳn vào service (không chỉ dựa `[Authorize]` controller, vốn cấp chung 1 danh sách role cho cả 4 action). Bảng `AllowedRolesByAction`: `Verify→QA,Admin` · `Approve→TrainingManager,Admin` · `Reject→QA,Admin` · `Return→QA,Admin`. Sai role → `ForbiddenAccessException` với message nêu rõ role nào không được phép làm action nào.
- **C6**: (a) `QA` được thêm vào role Reject/Return; (b) khi action là Reject hoặc Return, nếu `ETRCourseRecord.Status == "Submitted"`, tự động chuyển sang `"ReturnedForCorrection"` — trước đây chỉ nhánh Approve mới đụng tới `ETRCourseRecord`, khiến QA Reject xong mà hồ sơ vẫn kẹt "Submitted" mãi mãi.
- **[C5 bonus]**: mọi nhánh action nay đều ghi thêm 1 dòng `AuditLog` curated (ActionType/OldValue/NewValue/Description rõ ràng), bên cạnh `ApprovalHistory` đã có sẵn.

**Phát hiện quan trọng khi verify sống:** fix C6 ban đầu không hoạt động cho QA — `ApprovalsController`'s class-level `[Authorize(Roles = "Admin,Instructor,TrainingManager,Audit")]` thiếu `QA`, và ASP.NET Core kết hợp `[Authorize]` class-level + method-level theo **AND** chứ không phải OR — nên dù method-level đã cho QA, request vẫn bị 403 ngay từ class-level. Đã sửa class-level attribute thành `Admin,Instructor,QA,TrainingManager,Audit` để fix thực sự có hiệu lực. Đây là cùng dạng bug đã ghi nhận ở mục H1 của `LO_TRINH_HOAN_THIEN_DU_AN.md` (áp dụng cho `EtrController`) — chỉ sửa cho `ApprovalsController` lần này vì bắt buộc để C6 hoạt động, KHÔNG mở rộng sang `EtrController` (ngoài phạm vi C2-C8).

### 1.3 [C4] Wire up "Reopen" + audit

`IEtrService.LockEtrAsync`/`UnlockEtrAsync` tồn tại từ trước nhưng là dead code (không controller nào gọi). Đã thêm:
- Endpoint `POST /api/etr/{id}/reopen` (role `Admin`, tái dùng `ReturnEtrRequest` DTO cho field lý do bắt buộc).
- `UnlockEtrAsync` bắt buộc `reason` non-empty, guard `ETR phải đang Locked`, ghi `AuditLog` (ActionType="UNLOCK", có lý do).
- `LockEtrAsync` cũng ghi `AuditLog` tương tự (ActionType="LOCK").

**Bug runtime phát hiện + fix trong lúc verify sống:** gọi Reopen ban đầu luôn trả `500 ImmutabilityViolationException`. Nguyên nhân: `IGenericRepository<T>.Update(entity)` gọi `DbSet.Update()`, force-đánh dấu **toàn bộ** property của entity là `Modified` — kể cả khi entity đã được EF Core track sẵn từ `GetByIdAsync` trong cùng scope. Điều này phá vỡ điều kiện `IsBeingUnlocked` trong `ImmutabilityValidator` (yêu cầu **duy nhất** `IsLocked` được modified, còn `Status`/`SubmittedAt`/`VerifiedAt`/`CompletedAt`/`EnrollmentId` phải KHÔNG modified). Đã sửa: bỏ lời gọi `.Update(etr)` (thừa — entity đã tracked, EF Core tự phát hiện thay đổi qua `etr.IsLocked = false`).

**Giới hạn phát hiện thêm (đã ghi nhận minh bạch, KHÔNG tự ý mở rộng sửa)**: Reopen chỉ mở `IsLocked`, không đổi `Status` khỏi `"Completed"` — mà `ImmutabilityValidator.ValidateEtrChildEntity` chặn sửa entity con (Evidence/Attendance/AssessmentResult...) dựa trên `Status=="Completed" OR IsLocked==true`, KHÔNG có ngoại lệ cho nhánh này. Nghĩa là sau Reopen, người dùng sửa được `ETRCourseRecord` (VD đổi ghi chú), nhưng vẫn chưa sửa được dữ liệu con — cần 1 quyết định nghiệp vụ riêng (ETR nên về status nào khi Reopen?) trước khi mở rộng thêm. Đã ghi thành mục **H13 mới** trong `LO_TRINH_HOAN_THIEN_DU_AN.md`.

### 1.4 [C5] Bổ sung Audit Trail cho các service còn thiếu

Thêm `AuditLogRepository.AddAsync(...)` (curated, human-readable) vào:
- `AccountService`: `UpdateAccountStatusAsync`, `UpdateAccountRoleAsync`, `DeleteAccountAsync` (method này trước đây còn thiếu cả tham số actor — đã thêm `deletedByAccountId`).
- `EnrollmentService.DeleteEnrollmentAsync`.
- `CourseService`: Create/Update/Delete.
- `ClassService`: Create/Update/Delete.
- `ApprovalService.ProcessApprovalActionAsync` (mọi nhánh — xem 1.2).

**Điều chỉnh nhận định so với báo cáo gốc:** phát hiện `AppDbContext.SaveChangesAsync` đã có sẵn cơ chế **auto-capture** ghi `AuditLog` cho MỌI entity kế thừa `BaseEntity` khi Added/Modified (`AppDbContext.Compliance.cs: CapturePendingAuditEntries`) — nhận định gốc "hoàn toàn không có audit logging" chưa chính xác 100%. Đúng hơn: hệ thống đã có 1 lớp auto-capture generic (ActionType chung "INSERT"/"UPDATE", JSON đầy đủ mọi field, không có mô tả người đọc được), nhưng thiếu lớp log **curated/human-readable** riêng cho từng hành động nghiệp vụ — đúng như cách `EtrService` đã làm từ trước cho riêng ETR (SUBMIT/VERIFY/RETURN/APPROVE). Việc bổ sung lần này áp dụng NHẤT QUÁN pattern đã có sẵn đó cho các service còn lại, không phải thêm logging từ số 0.

### 1.5 [C7] Đảm bảo `Audit_History.pdf` (Training Package) luôn đủ bước Approve cuối

`EtrController.CompleteEtr` gọi thẳng `EtrService.CompleteEtrAsync` mà không đụng tới `ApprovalRequest`/`ApprovalHistory` — trong khi `ExportService.BuildAuditHistoryPdf` (tính năng CAA export vừa xong 2026-07-24) chỉ đọc từ `ApprovalHistory`. Nếu ETR hoàn tất qua route `complete` trực tiếp (không qua `Approvals/{id}/process?action=Approve`), file audit xuất ra thiếu đúng bước quan trọng nhất.
Đã sửa: `CompleteEtrAsync` tự tìm `ApprovalRequest` theo `ETRCourseRecordId`; nếu tồn tại và chưa `"Approved"`, tự cập nhật status + ghi 1 dòng `ApprovalHistory` (action="Approve", comment ghi rõ "completed directly via /api/etr/{id}/complete") ngay trong cùng transaction.

### 1.6 [C8] Phân công Giảng viên (Instructor) phụ trách Lớp học

Thêm `Class.InstructorAccountId` (nullable FK → `Account`, `DeleteBehavior.Restrict`), migration `AddInstructorAccountIdToClass`. Cập nhật `CreateClassRequest`/`UpdateClassRequest`/`TrainingClassResponse` + `ClassService`.
Validation nghiệp vụ: `InstructorAccountId` (nếu có) phải trỏ tới 1 Account tồn tại VÀ có Role chính xác là `"Instructor"` — không thì `BusinessRuleViolationException` rõ ràng, không cho gán bừa 1 accountId bất kỳ (kể cả Student/QA/Admin).

---

## 2. Đã kiểm chứng bằng cách nào

- `dotnet build` toàn solution sau mỗi batch sửa: **0 Error**.
- `dotnet test` (`ETR.Application.Tests`): **23/23 pass** — bao gồm 9 test mới (`EvidenceServiceTests.cs`: 2, `ApprovalServiceTests.cs`: 7, dùng Moq mock `IUnitOfWork`/`IEtrService`).
- Migration `AddInstructorAccountIdToClass` áp dụng sạch lên DB dev thật (Docker SQL Server) — additive, nullable, không phá dữ liệu cũ.
- Chạy full app thật + `curl`, đăng nhập đủ 7 role (Admin/Instructor/QA/Academic/TrainingManager/Student/Audit):
  - **C2**: Instructor tự verify → 403; Admin tự verify minh chứng chính mình upload → 403 đúng message; QA verify minh chứng của Instructor khác → 200.
  - **C3**: Instructor gọi `Approvals/process` bất kỳ action → 403; QA gọi `action=Approve` → 403; TrainingManager gọi `action=Verify` → 403 đúng message "Role 'TrainingManager' is not authorized to perform the 'Verify' action."
  - **C4**: reopen thiếu lý do → 400; reopen ETR đã Completed/Locked → 200, `isLocked: false`; reopen lần 2 → 400 "ETR is not locked."; AuditLog ghi nhận đúng.
  - **C5**: đổi status Account, tạo Class mới → mỗi hành động xuất hiện đúng 2 dòng AuditLog (1 curated + 1 auto-capture).
  - **C6**: QA gọi `action=Reject` (sau khi đã fix bug H1 cho `ApprovalsController`) → 200, `ApprovalRequest.CurrentStatus` chuyển "Approved"→"Rejected", AuditLog + ApprovalHistory đều ghi nhận.
  - **C8**: tạo Class với Instructor hợp lệ → 201; Instructor không tồn tại → 400; account tồn tại nhưng không phải role Instructor → 400 đúng message.
- **C7**: verify bằng code review kỹ (logic rõ ràng, guard đơn giản) — CHƯA verify bằng 1 lượt live đầy đủ Submit→Verify→Complete cho 1 ETR mới vì cần dàn dựng attendance/evidence/signoff cho đủ 4 subject (tốn thời gian hơn phạm vi lượt này) — ghi nhận minh bạch, không overclaim.

## 3. Rủi ro/việc còn lại

- **H13 (mới)**: Reopen (C4) chỉ mở `IsLocked`, chưa thực sự cho phép sửa dữ liệu con (Evidence/Attendance/AssessmentResult...) vì `Status` vẫn `"Completed"` — cần quyết định nghiệp vụ thêm trước khi mở rộng (xem `LO_TRINH_HOAN_THIEN_DU_AN.md` mục H13).
- **H1 (một phần)**: bug `[Authorize]` class/method AND-combination đã sửa cho `ApprovalsController` (bắt buộc để C6 chạy được) nhưng **`EtrController` vẫn còn cùng bug** — Academic vẫn không Submit/xem ETR được, TrainingManager vẫn không gọi trực tiếp được `CompleteEtr` (dù có đường vòng qua `Approvals`). Ngoài phạm vi C2-C8, đã ghi nhận lại trong roadmap.
- **C1, C9, C10** (đổi mật khẩu mock, secrets commit trong `appsettings.json`, `mock-admin-token` không cần auth) — chủ động **không làm** trong lượt này theo đúng yêu cầu phạm vi của người dùng.
- **H3** (`ImmutabilityViolationException` rơi vào 500 mơ hồ) vẫn còn mở — gặp lại nhiều lần trong lúc test lượt này (VD sửa evidence trên ETR đã Completed) — không nằm trong C2-C8 nhưng đáng ưu tiên sửa sớm vì rất dễ tái hiện.
- **H6**: điều kiện tiên quyết (Class.InstructorAccountId) đã có từ C8, nhưng logic scope "Instructor chỉ xem học viên lớp mình phụ trách" chưa được viết — cần 1 lượt riêng.

## 4. Files liên quan

| File | Trạng thái |
|---|---|
| `ETR.API/Controllers/EvidencesController.cs` | Sửa — role Verify còn QA,Admin |
| `ETR.Application/Services/EvidenceService.cs` | Sửa — guard self-verify |
| `ETR.API/Controllers/ApprovalsController.cs` | Sửa — class-level role list, pass RoleName vào service |
| `ETR.Application/Services/ApprovalService.cs` | Sửa — role-per-action, status sync Reject/Return, AuditLog |
| `ETR.Application/Interfaces/IApprovalService.cs` | Sửa — thêm tham số `actionByRoleName` |
| `ETR.API/Controllers/EtrController.cs` | Sửa — endpoint `reopen` mới |
| `ETR.Application/Services/EtrService.cs` | Sửa — Lock/UnlockEtrAsync có AuditLog + bỏ `.Update()` thừa; CompleteEtrAsync sync ApprovalHistory |
| `ETR.Application/Interfaces/IEtrService.cs` | Sửa — thêm tham số `reason` cho Lock/UnlockEtrAsync |
| `ETR.API/Controllers/AccountsController.cs` | Sửa — truyền actor vào DeleteAccountAsync |
| `ETR.Application/Services/AccountService.cs` | Sửa — AuditLog cho status/role/delete |
| `ETR.Application/Interfaces/IAccountService.cs` | Sửa — thêm tham số `deletedByAccountId` |
| `ETR.Application/Services/EnrollmentService.cs` | Sửa — AuditLog cho delete |
| `ETR.Application/Services/CourseService.cs` | Sửa — AuditLog cho Create/Update/Delete |
| `ETR.Application/Services/ClassService.cs` | Sửa — AuditLog + InstructorAccountId + validation |
| `ETR.Domain/Entities/Class.cs` | Sửa — thêm `InstructorAccountId` |
| `ETR.Infrastructure/Data/AppDbContext.cs` | Sửa — FK config cho `Class.InstructorAccountId` |
| `ETR.Infrastructure/Migrations/*_AddInstructorAccountIdToClass.cs` | Mới |
| `ETR.Application/DTOs/Class/Requests/CreateClassRequest.cs` | Sửa — thêm `InstructorAccountId` |
| `ETR.Application/DTOs/Class/Requests/UpdateClassRequest.cs` | Sửa — thêm `InstructorAccountId` |
| `ETR.Application/DTOs/Class/Responses/TrainingClassResponse.cs` | Sửa — thêm `InstructorAccountId` |
| `ETR.Application.Tests/ETR.Application.Tests.csproj` | Sửa — thêm Moq |
| `ETR.Application.Tests/Services/EvidenceServiceTests.cs` | Mới |
| `ETR.Application.Tests/Services/ApprovalServiceTests.cs` | Mới |
| `ETR.Documentation/LO_TRINH_HOAN_THIEN_DU_AN.md` | Sửa — cập nhật trạng thái C2-C8, thêm H13 |
