# Fix P2: CompletionRequirement Gate, Export thật (QuestPDF+ZIP), Dashboard/Report KPI, Retake Limit — 2026-07-21

**Ngày thực hiện:** 2026-07-21
**Phạm vi:** `EtrService.cs`, `ApprovalService.cs`, `AssessmentResultService.cs`, `AttendanceService.cs`, `PracticalChecklistResultService.cs`, `CompletionRequirementService.cs`, mới: `ExportService.cs`, `DashboardKpiCalculator.cs`, `EtrController.cs`, `ExportsController.cs`, `DashboardController.cs`, `ReportsController.cs`, `SearchController.cs`, `DataSeeder.cs`, `AppDbContext.cs`, `Program.cs`, entity `CompletionRequirement.cs`/`ExportJob.cs`, 2 migration mới.
**Mục tiêu:** Vá 12/14 mục P2 trong `ETR.Documentation/BAO_CAO_REVIEW_HE_THONG.md` (mục "🎯 Danh sách công việc ưu tiên" → "P2 × Rủi ro Trung bình — sprint kế tiếp", mục 21-34) — tiếp nối batch P0 (2026-07-20) và P1 (2026-07-21) đã xong. Mục 28 và 29 chủ động **không sửa code** theo quyết định của user (xem mục 3).

---

## 1. Tóm tắt những gì đã implement

### 1.1 [Mục 21] Sửa công thức `AttendanceRate`

Trước: `AttendanceService.RecordAttendanceAsync` chia số buổi `Present` cho **tổng số session đã lên lịch** của Subject+Class (kể cả session tương lai chưa diễn ra) — tỷ lệ bị pha loãng sai trong suốt khoá học.
Đã sửa: mẫu số nay chỉ tính session có `IsConfirmed == true` (đã thực sự diễn ra/đã chốt), qua helper dùng chung `RecalculateAttendanceRateAsync`.

### 1.2 [Mục 22] `EvaluateSubjectPassabilityAsync` gán rõ `Status="Failed"`

Trước: mỗi điều kiện không đạt (attendance, checklist, evidence, score) chỉ `return;` sớm, để `SubjectResult.Status` kẹt ở `"Pending"` vĩnh viễn — không phân biệt được "chưa đủ điều kiện" với "đã chấm nhưng rớt".
Đã sửa: viết lại thành biến `isPassable`, đánh giá đủ 4 điều kiện, cuối cùng `subjectResult.Status = isPassable ? "Passed" : "Failed"` — luôn có kết luận rõ ràng sau mỗi lần Signoff.

### 1.3 [Mục 23] Nối `CompletionRequirement` vào Submit-gate (schema mới)

Đây là mục lớn nhất — theo lựa chọn của user, đã thêm cấu trúc dữ liệu thật thay vì chỉ đọc text tự do:
- Entity `CompletionRequirement` thêm 2 cột: `RequirementType` (string: `"MinAttendance"` | `"AllAssessmentsPassed"` | `"AllChecklistsSignedOff"` | `null` = chỉ mô tả, không enforce) và `ThresholdValue` (decimal, dùng cho `MinAttendance`).
- Migration mới `AddCompletionRequirementTypeAndThreshold` (2 cột nullable, không phá dữ liệu cũ — các requirement đã tạo trước đây có `RequirementType=null` nên vẫn chỉ mang tính mô tả như trước, không tự nhiên bị enforce ngoài ý muốn).
- `EtrService.SubmitEtrAsync` thêm **gate #5**: lấy toàn bộ `CompletionRequirement` mandatory của Course, `switch` theo `RequirementType` để enforce đúng loại (attendance tối thiểu / tất cả assessment Passed-Exempted / tất cả checklist bắt buộc đã Signed Off).
- `CreateCompletionRequirementRequest`/`UpdateCompletionRequirementRequest`/`CompletionRequirementResponse` + `CompletionRequirementService` cập nhật để nhận/trả 2 field mới.
- `DataSeeder.cs`: gán đúng `RequirementType`/`ThresholdValue` cho 3 requirement mẫu (`"Minimum 80% Attendance"` → `MinAttendance`/80, `"All Assessments Passed"` → `AllAssessmentsPassed`, `"All Practical Checklists Signed Off"` → `AllChecklistsSignedOff`) — tính năng CRUD từng "đảo hoang" nay thật sự có tác dụng ngay từ dữ liệu mẫu.

### 1.4 [Mục 24] Giới hạn Retake + người duyệt khác người ghi điểm

`AssessmentResultService.RecordAssessmentScoreAsync`: thêm hằng số `BusinessRuleEngine.MaxAssessmentAttempts = 3` (tổng số lần thi kể cả lần đầu) — vượt quá → `InvalidOperationException`. Thêm field `AuthorizedByAccountId` vào `CreateAssessmentResultRequest`; khi là lần retake (có `existingResult`), bắt buộc `AuthorizedByAccountId` khác với người gọi API (`recordedByAccountId`) — nếu không → lỗi. `RetakeHistory.AuthorizedByAccountId` giờ dùng đúng giá trị người duyệt thay vì trùng người ghi điểm.

### 1.5 [Mục 25] Training Package export thật (ZIP + PDF + audit trail)

Theo lựa chọn của user (thêm library PDF thật): đã thêm package `QuestPDF` (2026.7.1, license `Community` khai báo trong `Program.cs`).
- File mới `ExportService.cs` (+ `IExportService`): `ExportTrainingPackageAsync` — gate `ETR.Status=="Completed"` mới cho export, dựng PDF thật gồm: ETR summary (ID/học viên/lớp/status/mốc thời gian), bảng Subject Results (mã môn, tên, status, điểm, attendance %), bảng Evidence Files (tên file, verification status, verified at), bảng Approval Audit Trail (action, chuyển trạng thái, thời gian, comment) — rồi nén vào 1 file `.zip` thật (dùng `System.IO.Compression`, không cần thêm package).
- Entity `ExportJob` thêm cột `ETRCourseRecordId` (migration mới `AddETRCourseRecordIdToExportJob`) — gắn đúng ETR nguồn vào job export, trước đây hoàn toàn không có liên kết.
- `ExportsController.ExportTrainingPackage`: nhận `ETRCourseRecordId` qua `ExportRequest`, gọi `ExportService`, trả `ExportJobResponse` có `ETRCourseRecordId`.
- `ExportsController.DownloadExportFile`: đọc file ZIP thật từ đĩa nếu tồn tại (dùng `PhysicalFile`), fallback về nội dung mock cũ cho 2 loại export khác (`PDF`, `Dashboard` — **vẫn còn mock**, ngoài phạm vi mục 25 chỉ nói "Training Package").
- **Đã verify end-to-end bằng `curl` thật**: gọi export → nhận `ExportJobResponse` với `ETRCourseRecordId` đúng → tải file qua `download/{id}` → giải nén → xác nhận PDF hợp lệ (1 trang, PDF 1.4).

### 1.6 [Mục 26] Mở rộng Dashboard/Report KPI

Trước: cả 2 controller chỉ trả `TotalClasses`/`TotalEtrs`. File mới `DashboardKpiCalculator.cs` (static helper dùng chung, tránh trùng lặp logic giữa 2 controller) tính thêm: `CompletedCount`, `CompletionRatePercent`, `PendingApprovalCount` (Status Submitted/Verified), `RejectedCount` (từ `ApprovalRequest.CurrentStatus=="Rejected"`), `ReturnedForCorrectionCount`, `MissingEvidenceCount` (ETR có SubjectResult nào chưa đủ evidence Verified, chưa Completed). `DashboardController`/`ReportsController` đều gọi chung helper này.

### 1.7 [Mục 27] `SearchController.SearchEtrs` filter thật + scope theo role

Trước: bỏ qua hoàn toàn `query`, trả tất cả ETR cho bất kỳ ai. Đã sửa: filter theo `query` khớp `Status`, `ETRCourseRecordId`, hoặc `FullName` của học viên (qua `CourseEnrollment`→`UserProfile`); nếu role gọi là `Student`, tự động scope chỉ trong ETR của chính mình trước khi filter.

### 1.8 [Mục 30] Bắt buộc `comment` khi Reject/Return

`EtrService.ReturnEtrAsync`: throw `ValidationException` nếu `comment` rỗng/null/whitespace. `ApprovalService.ProcessApprovalActionAsync`: cùng validate khi `action` là `"Reject"` hoặc `"Return"`, kiểm tra trước khi mở transaction.

### 1.9 [Mục 31] Tự động tạo `ApprovalRequest` khi Submit thành công

`CreateApprovalRequestAsync` tồn tại từ trước nhưng không ai gọi. **Không** wiring trực tiếp trong `EtrService` (sẽ tạo circular DI vì `ApprovalService` đã phụ thuộc `IEtrService` từ batch P0) — thay vào đó `EtrController.SubmitEtr` gọi tuần tự: `EtrService.SubmitEtrAsync` rồi `ApprovalService.CreateApprovalRequestAsync(id, currentApproverId: null, accountId, ct)` ngay sau khi Submit thành công.

### 1.10 [Mục 32-34] (đã implement trong lượt trước của cùng batch P2, liệt kê lại cho đầy đủ)

- **32**: `AttendanceService.UpdateAttendanceRecordAsync`/`DeleteAttendanceRecordAsync` nay tính lại `AttendanceRate` sau khi sửa/xoá (trước đây chỉ `RecordAttendanceAsync` mới tính), và cả 2 method nay được bọc transaction (2 write cùng lúc: AttendanceRecord + SubjectResult).
- **33**: Ngưỡng Pass của Practical Checklist đọc từ `CourseSubject.PassingScore` thay vì hard-code `50` — sửa ở cả `AssessmentResultService.EvaluateSubjectPassabilityAsync` và `PracticalChecklistResultService` (2 chỗ đều từng hard-code).
- **34**: `PracticalChecklistResultService.CreatePracticalChecklistResultAsync` nay `throw InvalidOperationException` rõ ràng nếu chưa có `PracticalChecklist` được cấu hình cho Course+Subject, thay vì âm thầm tự tạo 1 checklist giả.

---

## 2. Đã kiểm chứng bằng cách nào

- `dotnet build` toàn solution: **0 Error** (cùng vài warning `CS8604`/`NU1510` có sẵn từ trước, không liên quan).
- 2 migration mới tạo bằng `dotnet ef migrations add` (không sửa tay migration cũ), cả 2 đều additive/nullable — an toàn cho DB đã tồn tại dữ liệu.
- Chạy full app thật (`dotnet run`, SQL Server Docker) + `curl`:
  - `POST /api/auth/login` (tài khoản seed, mật khẩu hash BCrypt từ batch P0) → 200.
  - `GET /api/dashboard/stats` và `GET /api/reports/summary` → trả đủ 8 field KPI với số liệu đúng (`completionRatePercent=100` khớp ETR mẫu đã Completed).
  - `GET /api/search/etrs?query=Completed` → lọc đúng, không còn trả toàn bộ bảng vô điều kiện.
  - `POST /api/exports/training-package` (ETR mẫu, đã Completed) → tạo `ExportJob` thật với `ETRCourseRecordId` đúng.
  - `GET /api/exports/download/{id}` → tải về, giải nén, xác nhận file `.pdf` bên trong hợp lệ (`file` báo "PDF document, version 1.4, 1 pages").
- Không có test project trong repo — không verify được item 23/24/30/31 bằng gọi API thật trong lượt này (cần dàn dựng state ETR ở nhiều giai đoạn khác nhau, tốn thời gian hơn phạm vi 1 lượt smoke test) — chỉ verify bằng code review + build.

## 3. Quyết định không sửa code (theo yêu cầu user) & rủi ro còn lại

- **[Mục 28] Đồng nhất transaction pattern cho 9 service CRUD đơn giản** — **chủ động bỏ qua**. Mỗi service (`AccountService`, `CourseService`, `ClassService`, `SubjectService`, `DepartmentService`, `EvidenceTypeService`, `CompletionRequirementService`, `PracticalChecklistService`, và phần còn lại của `EtrService`) hiện chỉ gọi **một** `SaveChangesAsync` mỗi method — EF Core đã tự bọc transaction ngầm cho 1 lần `SaveChanges`, nên bọc thêm `Begin/Commit` tường minh không sửa bug thật nào, chỉ là nhất quán style. Diff sẽ rất lớn (9 file) cho giá trị thấp — user đồng ý bỏ qua.
- **[Mục 29] Sửa migration `sp_MSForEachTable DELETE` thành idempotent** — **chủ động không sửa file migration cũ**. Đây là migration `SeedSystemData` (2026-07-19) **đã áp dụng** — sửa giờ không có tác dụng gì lên DB đã tồn tại, và vi phạm nguyên tắc "không sửa migration đã ship". Bug reseed-về-0 nó từng gây ra đã được migration sau (`FixSchemaDriftAndIdentitySeed`, xem `docs/maintain/2026-07-20_data-seeder-va-fix-schema-migration.md`) trung hoà hoàn toàn cho mọi DB mới from-scratch — đã tự verify lại điều này ở batch trước (P0) bằng cách connect SQL Server thật.
- **PDF/Dashboard export (2 trong 3 loại export)** vẫn còn mock — mục 25 chỉ yêu cầu "Training Package" thật, không mở rộng phạm vi.
- **Item 23 mới enforce 3 loại `RequirementType`** — bất kỳ `CompletionRequirement` nào tạo sau này với `RequirementType` khác (hoặc để trống) sẽ **không** được Submit-gate enforce tự động, chỉ mang tính mô tả — cần dùng đúng 1 trong 3 giá trị string để có tác dụng thật.
- **Giới hạn `MaxAssessmentAttempts=3`** (mục 24) là giá trị mặc định tự chọn hợp lý (báo cáo gốc không chỉ định số cụ thể) — cần xác nhận lại với business nếu muốn số khác.

## 4. Files liên quan

| File | Trạng thái |
|---|---|
| `ETR.Application/Interfaces/IExportService.cs` | Mới |
| `ETR.Application/Services/ExportService.cs` | Mới |
| `ETR.Application/Services/DashboardKpiCalculator.cs` | Mới |
| `ETR.Infrastructure/Migrations/*_AddCompletionRequirementTypeAndThreshold.cs` | Mới |
| `ETR.Infrastructure/Migrations/*_AddETRCourseRecordIdToExportJob.cs` | Mới |
| `ETR.Domain/Entities/CompletionRequirement.cs` | Sửa — thêm `RequirementType`, `ThresholdValue` |
| `ETR.Domain/Entities/ExportJob.cs` | Sửa — thêm `ETRCourseRecordId` |
| `ETR.Application/Compliance/BusinessRuleEngine.cs` | Sửa — thêm `MaxAssessmentAttempts` |
| `ETR.Application/Services/EtrService.cs` | Sửa — gate #5 CompletionRequirement, comment bắt buộc ở Return |
| `ETR.Application/Services/ApprovalService.cs` | Sửa — comment bắt buộc Reject/Return |
| `ETR.Application/Services/AssessmentResultService.cs` | Sửa — Failed status, PassingScore động, retake limit + approver |
| `ETR.Application/Services/AttendanceService.cs` | Sửa — công thức AttendanceRate, recalc ở Update/Delete |
| `ETR.Application/Services/PracticalChecklistResultService.cs` | Sửa — PassingScore động, throw thay vì auto-create |
| `ETR.Application/Services/CompletionRequirementService.cs` | Sửa — map field mới |
| `ETR.Application/DependencyInjection.cs` | Sửa — đăng ký `IExportService` |
| `ETR.Application/DTOs/CompletionRequirement/*.cs` (3 file) | Sửa — thêm field mới |
| `ETR.Application/DTOs/Assessment/Requests/CreateAssessmentResultRequest.cs` | Sửa — thêm `AuthorizedByAccountId` |
| `ETR.Application/DTOs/Export/Requests/ExportRequest.cs` | Sửa — thêm `ETRCourseRecordId` |
| `ETR.Application/DTOs/Export/Responses/ExportJobResponse.cs` | Sửa — thêm `ETRCourseRecordId` |
| `ETR.Application/ETR.Application.csproj` | Thêm `PackageReference QuestPDF` |
| `ETR.Infrastructure/Data/AppDbContext.cs` | Sửa — precision cho `ThresholdValue` |
| `ETR.Infrastructure/Data/DataSeeder.cs` | Sửa — gán `RequirementType`/`ThresholdValue` cho seed data |
| `ETR.API/Controllers/EtrController.cs` | Sửa — auto-create ApprovalRequest sau Submit |
| `ETR.API/Controllers/ExportsController.cs` | Sửa — export thật + download file thật |
| `ETR.API/Controllers/DashboardController.cs` | Sửa — dùng `DashboardKpiCalculator` |
| `ETR.API/Controllers/ReportsController.cs` | Sửa — dùng `DashboardKpiCalculator` |
| `ETR.API/Controllers/SearchController.cs` | Sửa — filter + scope thật |
| `ETR.API/Program.cs` | Sửa — khai báo QuestPDF license |
