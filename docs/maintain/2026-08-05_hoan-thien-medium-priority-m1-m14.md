# Hoàn thiện các mục Medium-priority (M1-M11, M14) — 2026-08-05

**Ngày thực hiện:** 2026-08-05
**Phạm vi:** `ETR.API/Controllers/{ClassesController,CoursesController,AttendanceController,DashboardController,UserProfilesController,ExportsController}.cs`; xoá `ReportsController.cs`; `ETR.Application/Services/{EnrollmentService,AttendanceService,DashboardKpiCalculator,UserProfileService,EtrService}.cs`; `ETR.Application/Interfaces/{IAttendanceService,IUserProfileService,IEtrService}.cs`; `ETR.Application/DTOs/{Attendance/*,UserProfile/UserProfileDtos,Etr/Responses/EtrCompletionProgressResponse}.cs` (+ 2 DTO mới); `ETR.Domain/Entities/UserProfile.cs`; xoá `ETR.Domain/Entities/DashboardSnapshot.cs`; `ETR.Application/Interfaces/IUnitOfWork.cs`, `ETR.Infrastructure/Repositories/UnitOfWork.cs`, `ETR.Infrastructure/Data/AppDbContext.cs`; migration mới `AddUserProfileStatusAndRemoveDashboardSnapshot`.
**Mục tiêu:** `/mpower:code-fix` cho 12 mục Medium trong `ETR.Documentation/LO_TRINH_HOAN_THIEN_DU_AN.md`: M1, M2, M3, M4, M5, M6, M7, M8, M9, M10, M11, M14.

---

## 1. M1 — Không có field `Status` chung cho học viên

`UserProfile` trước chỉ có status theo từng `Enrollment`, không có trạng thái tổng quát (Active/Withdrawn/Graduated) cho chính người học.

**Đã sửa:** thêm `UserProfile.Status` (mặc định `"Active"`), expose qua `UserProfileResponse`. Thêm endpoint riêng `PUT /api/userprofiles/{accountId}/status` (Admin,Academic — mirror đúng pattern `AccountService.UpdateAccountStatusAsync`, có audit log), validate whitelist `Active|Withdrawn|Graduated`. Không gộp vào `UpdateUserProfileAsync` chung để việc đổi status luôn tường minh/có chủ đích, có audit trail riêng.

**Verify sống:** `GET /api/userprofiles/me` trả `"status":"Active"`.

## 2. M2 — Huỷ ghi danh (Enrollment) không cascade sang ETR/ClassStudent

`EnrollmentService.DeleteEnrollmentAsync` chỉ soft-delete `CourseEnrollment`, để lại `ETRCourseRecord`/`ClassStudent` liên quan ở trạng thái "còn hoạt động" trỏ tới 1 enrollment đã bị huỷ — dữ liệu mồ côi.

**Đã sửa:** khi huỷ enrollment (chỉ được phép khi ETR đang Draft/InProgress — guard này đã có sẵn từ trước), cascade: `Enrollment.Status → "Withdrawn"`, `ETRCourseRecord.Status → "Cancelled"` + soft-delete, `ClassStudent.Status → "Withdrawn"` + soft-delete — tất cả trong cùng 1 lượt ghi.

**Verify sống:** xoá Enrollment #2 (ETR #2 đang InProgress) → `204`; kiểm tra DB: `ETRCourseRecords#2.Status = "Cancelled", IsDeleted=1`; `ClassStudents#2.Status = "Withdrawn", IsDeleted=1`; `CourseEnrollments#2.Status = "Withdrawn", IsDeleted=1`.

## 3. M3 — TrainingManager có toàn quyền CRUD Course/Class, vượt phạm vi FRD

FRD mô tả TrainingManager là vai trò "duyệt/giám sát", không phải quản trị dữ liệu gốc — nhưng `ClassesController`/`CoursesController` cho TrainingManager cả Create/Update/Delete.

**Đã sửa:** bỏ `TrainingManager` khỏi role list của Create/Update/Delete trên cả 2 controller (còn `Admin,Academic`). Quyền đọc (GET) giữ nguyên cho TrainingManager — vai trò giám sát vẫn cần xem được dữ liệu.

**Verify sống:** TrainingManager gọi `POST /api/classes`, `POST /api/courses` → `403` cả 2; `GET /api/classes` → `200` (không đổi).

## 4. M4 — Không có cơ chế nhắc nhở khi tỷ lệ điểm danh thấp

FRD muốn cảnh báo khi điểm danh thấp, nhưng repo không có hạ tầng email/push notification nào — xây dựng kênh gửi thông báo thật là phạm vi lớn hơn 1 lượt code-fix (cần thêm hạ tầng mới, đúng như ghi chú gốc của mục này).

**Đã sửa (phạm vi thực tế, không tự ý mở rộng thêm hạ tầng gửi thông báo):** thêm `GET /api/attendance/low-attendance?classId=` trả danh sách học viên có `AttendanceRate` dưới ngưỡng `BusinessRuleEngine.MinimumAttendanceThreshold` (80%), kèm mã/tên học viên, lớp, môn học. Đây là dữ liệu nền để FE tự dựng UI nhắc nhở (badge, danh sách "cần theo dõi") — không tự động gửi email/push.

**Verify sống:** `GET /api/attendance/low-attendance` → `200`, `[]` (không có học viên nào dưới ngưỡng trong seed data hiện tại — đúng kỳ vọng).

## 5. M5 — Trạng thái điểm danh là free-text không validate

`CreateAttendanceRecordRequest`/`UpdateAttendanceRecordRequest.Status` trước chỉ giới hạn `[MaxLength(20)]`, không chặn gõ sai (VD `"Present "`, `"present"`, `"Absnet"`) làm sai lệch tỷ lệ điểm danh (so khớp chuỗi chính xác `"Present"` ở `AttendanceService`).

**Đã sửa:** thêm `[RegularExpression("^(Present|Absent|Late)$")]` trên cả 2 DTO.

**Verify sống:** `POST /api/attendance/record` với `status:"BunkedOff"` → `400` "Status must be one of: Present, Absent, Late."

## 6. M6 — "Xác nhận điểm danh" không thực sự khoá sửa/xoá bản ghi sau đó

`UpdateAttendanceRecordAsync`/`DeleteAttendanceRecordAsync` không kiểm tra `Session.IsConfirmed` — sửa/xoá điểm danh sau khi buổi học đã "chốt" vẫn thành công, làm mất ý nghĩa của bước xác nhận.

**Đã sửa:** cả 2 method thêm guard — nếu `Session.IsConfirmed == true` → `BusinessRuleViolationException`.

**Verify sống:** `PUT /api/attendance/1` (thuộc session đã confirm) → `400` "Cannot modify an attendance record for a session that has already been confirmed."

## 7. M7 — QA không có quyền xem dữ liệu điểm danh

`AttendanceController` không có role QA ở bất kỳ action GET nào — QA không xác minh được tính xác thực của dữ liệu điểm danh.

**Đã sửa:** thêm `QA` vào role list của `GetAllRecords`, `GetRecordById`, và endpoint `low-attendance` mới (mục 4).

**Verify sống:** QA gọi `GET /api/attendance` → `200`.

## 8. M8 — Không có API xem % tiến độ đáp ứng điều kiện hoàn thành trước khi Submit

Trước đây học viên/giảng viên chỉ biết ETR có đủ điều kiện Submit hay không SAU KHI gọi Submit và nhận lỗi — không có cách xem trước.

**Đã sửa:** thêm `GET /api/etr/{id}/completion-progress` — `EtrService.GetCompletionProgressAsync` lặp lại chính xác 5 nhóm kiểm tra của `SubmitEtrAsync` (mandatory subjects Passed/Exempted, attendance ≥ ngưỡng, evidence Verified, instructor signoff, `CompletionRequirement` tuỳ course) nhưng **không ném lỗi** — trả về danh sách từng check kèm trạng thái đạt/chưa đạt và % tổng thể. Chỉ đọc, không đổi state ETR.

**Verify sống:** `GET /api/etr/1/completion-progress` → `200`, `{"totalChecks":16,"metChecks":14,"percentComplete":87.5,"checks":[...]}`.

## 9. M9 — Dashboard chỉ trả số đếm, không trả danh sách ID để "đôn đốc"

`DashboardKpiCalculator` (dùng chung cho Dashboard) chỉ trả `PendingApprovalCount`/`RejectedCount`/`MissingEvidenceCount` — muốn biết CỤ THỂ ETR nào phải tra riêng.

**Đã sửa:** thêm `DashboardKpiCalculator.ComputeActionItemsAsync` + `GET /api/dashboard/action-items` trả 4 danh sách ID (`pendingApprovalEtrIds`, `rejectedEtrIds`, `returnedForCorrectionEtrIds`, `missingEvidenceEtrIds`) — endpoint `stats` cũ giữ nguyên không đổi (tránh phá vỡ consumer hiện có).

**Verify sống:** `GET /api/dashboard/action-items` → `{"pendingApprovalEtrIds":[1],"rejectedEtrIds":[1],"returnedForCorrectionEtrIds":[],"missingEvidenceEtrIds":[1,2]}`.

## 10. M10 — `ReportsController` và `DashboardController` là 2 implementation giống hệt nhau

Cả 2 controller gọi chung `DashboardKpiCalculator`, trả về đúng 1 shape response — xác nhận trùng lặp 100%, không có sự khác biệt nào theo vai trò màn hình.

**Đã sửa (theo đúng chỉ định — xoá `ReportsController`):** xoá hẳn `ReportsController.cs`. Cập nhật comment trong `DashboardKpiCalculator` ("used by DashboardController" thay vì "cả 2 controller").

**Verify sống:** `GET /api/reports/summary` → `404` (route không còn tồn tại); `GET /api/dashboard/stats` vẫn `200` như cũ.

## 11. M11 — `DashboardSnapshot` là scaffold chết, chưa từng được ghi/đọc

Entity + bảng `DashboardSnapshots` tồn tại từ trước nhưng 0 tham chiếu ngoài khai báo `DbSet`/repository — theo dõi xu hướng Reject theo thời gian nhưng chưa bao giờ implement job ghi dữ liệu.

**Đã sửa (theo đúng chỉ định — xoá):** xoá `DashboardSnapshot.cs`, `IUnitOfWork.DashboardSnapshotRepository`, implementation trong `UnitOfWork.cs`, `DbSet`/Fluent config trong `AppDbContext.cs`. Sinh migration `AddUserProfileStatusAndRemoveDashboardSnapshot` (gộp chung với M1 vì cùng 1 lượt migration) — `DROP TABLE DashboardSnapshots` + `ADD COLUMN UserProfiles.Status`.

**Verify sống:** `dotnet ef database update` chạy sạch trên DB dev thật; API khởi động và hoạt động bình thường sau khi xoá.

## 12. M14 — Endpoint tải Training Package không kiểm tra hạn tải

`DownloadExportFile` không kiểm tra `ExportJob.DownloadExpiredAt` — file vẫn tải được mãi dù đã "hết hạn" theo dữ liệu ghi nhận.

**Đã sửa:** thêm check `DateTime.UtcNow > job.DownloadExpiredAt` → trả `410 Gone` kèm thời điểm hết hạn, trước khi đọc file vật lý.

**Verify sống:** set `DownloadExpiredAt` của 1 export job về quá khứ (`2020-01-01`) → `GET /api/exports/download/{id}` → `410 Gone`, đúng thời điểm hết hạn trong message.

---

## 13. Đã kiểm chứng bằng cách nào

- `dotnet build` toàn solution: **0 Error** (2 warning có sẵn từ trước, không liên quan).
- `dotnet test` (`ETR.Application.Tests`): **23/23 pass**, không có regression.
- Migration `AddUserProfileStatusAndRemoveDashboardSnapshot` áp dụng sạch lên DB dev thật (Docker SQL Server) — `dotnet ef database update` không lỗi.
- Chạy app thật, `curl` trực tiếp từng endpoint cho cả 12 mục — chi tiết per-mục ở trên, bao gồm cả việc xác nhận dữ liệu cascade (M2) và trạng thái DB sau khi xoá (M11) bằng truy vấn SQL trực tiếp, không chỉ tin theo response API.

## 14. Rủi ro/việc còn lại

- **M4**: chỉ mới có endpoint DỮ LIỆU cho reminder — chưa có cơ chế gửi thông báo thật (email/push/in-app notification). Nếu nghiệp vụ cần gửi chủ động, cần 1 lượt riêng để chọn + tích hợp kênh gửi (ngoài phạm vi 1 lượt code-fix).
- **M9**: `stats` cũ và `action-items` mới hiện là 2 endpoint riêng biệt (không hợp nhất) để tránh phá vỡ bất kỳ consumer nào đang dùng shape cũ của `stats` — nếu FE muốn 1 endpoint duy nhất trả cả 2, cần xác nhận thêm.
- **M10**: xoá hẳn `ReportsController` nghĩa là bất kỳ FE screen nào đang gọi `/api/reports/summary` sẽ vỡ (404) — đã xác nhận với yêu cầu người dùng trước khi xoá, nhưng cần thông báo FE team đổi sang gọi `/api/dashboard/stats` (cùng response shape).
- Các mục M12, M13, M15, M18 và toàn bộ nhóm Low (L1-L10) không thuộc phạm vi yêu cầu lần này — không đụng tới.

## 15. Files liên quan

| File | Trạng thái |
|---|---|
| `ETR.Domain/Entities/UserProfile.cs` | Sửa — thêm `Status` |
| `ETR.Domain/Entities/DashboardSnapshot.cs` | Xoá |
| `ETR.Application/DTOs/UserProfile/UserProfileDtos.cs` | Sửa — thêm `Status` vào response, DTO mới `UpdateUserProfileStatusRequest` |
| `ETR.Application/Interfaces/IUserProfileService.cs` | Sửa — thêm `UpdateProfileStatusAsync` |
| `ETR.Application/Services/UserProfileService.cs` | Sửa — implement `UpdateProfileStatusAsync`, map `Status` |
| `ETR.API/Controllers/UserProfilesController.cs` | Sửa — endpoint mới `PUT {accountId}/status` |
| `ETR.Application/Services/EnrollmentService.cs` | Sửa — cascade Delete sang ETR/ClassStudent |
| `ETR.API/Controllers/ClassesController.cs` | Sửa — bỏ TrainingManager khỏi Create/Update/Delete |
| `ETR.API/Controllers/CoursesController.cs` | Sửa — tương tự Classes |
| `ETR.Application/DTOs/Attendance/Responses/LowAttendanceStudentResponse.cs` | Mới |
| `ETR.Application/Interfaces/IAttendanceService.cs` | Sửa — thêm `GetLowAttendanceStudentsAsync` |
| `ETR.Application/Services/AttendanceService.cs` | Sửa — implement low-attendance query, guard confirmed-session edit/delete |
| `ETR.Application/DTOs/Attendance/Requests/CreateAttendanceRecordRequest.cs` | Sửa — whitelist validate Status |
| `ETR.Application/DTOs/Attendance/Requests/UpdateAttendanceRecordRequest.cs` | Sửa — whitelist validate Status |
| `ETR.API/Controllers/AttendanceController.cs` | Sửa — thêm QA vào role list, endpoint mới `low-attendance` |
| `ETR.Application/DTOs/Etr/Responses/EtrCompletionProgressResponse.cs` | Mới |
| `ETR.Application/Interfaces/IEtrService.cs` | Sửa — thêm `GetCompletionProgressAsync` |
| `ETR.Application/Services/EtrService.cs` | Sửa — implement `GetCompletionProgressAsync` |
| `ETR.API/Controllers/EtrController.cs` | Sửa — endpoint mới `GET {id}/completion-progress` |
| `ETR.Application/Services/DashboardKpiCalculator.cs` | Sửa — thêm `DashboardActionItems`/`ComputeActionItemsAsync` |
| `ETR.API/Controllers/DashboardController.cs` | Sửa — endpoint mới `action-items` |
| `ETR.API/Controllers/ReportsController.cs` | Xoá |
| `ETR.Application/Interfaces/IUnitOfWork.cs` | Sửa — bỏ `DashboardSnapshotRepository` |
| `ETR.Infrastructure/Repositories/UnitOfWork.cs` | Sửa — bỏ implementation tương ứng |
| `ETR.Infrastructure/Data/AppDbContext.cs` | Sửa — bỏ `DbSet`/Fluent config `DashboardSnapshot` |
| `ETR.Infrastructure/Migrations/*_AddUserProfileStatusAndRemoveDashboardSnapshot.cs` | Mới |
| `ETR.API/Controllers/ExportsController.cs` | Sửa — check `DownloadExpiredAt` → 410 |
| `ETR.Documentation/LO_TRINH_HOAN_THIEN_DU_AN.md` | Sửa — đánh dấu M1-M11, M14 đã fix |
