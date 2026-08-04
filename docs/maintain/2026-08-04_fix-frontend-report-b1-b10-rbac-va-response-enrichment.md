# Fix Frontend Report B1→B10: RBAC gaps, endpoint mới, làm giàu response — 2026-08-04

**Ngày thực hiện:** 2026-08-04 · **Cập nhật (review pass):** 2026-08-04 — sau khi merge một batch fix độc lập từ `main` (cùng xử lý report_20260803.md), chạy `/mpower:flow-review-code` đối chiếu lại toàn bộ diff `ec89bc9..HEAD` để xác nhận cả 10 mục B1-B10 vẫn đúng sau merge, và fix thêm các gap phát sinh — xem mục 2b.
**Phạm vi:** `ETR.API/Controllers/EtrController.cs`, `ClassesController.cs`, `CoursesController.cs`, `SubjectsController.cs`, `EnrollmentsController.cs`, `UserProfilesController.cs`, `AuditController.cs`, `EvidenceTypesController.cs`, `EvidencesController.cs`, `AccountsController.cs`, `SearchController.cs`; `ETR.Application/Services/AccountService.cs`, `EtrService.cs`, `EvidenceService.cs`; `ETR.Application/Interfaces/IAccountService.cs`; `ETR.Application/DTOs/Account/AccountDtos.cs`, `Etr/Responses/SubjectResultResponse.cs`; mới: `ETR.Application/DTOs/Search/EtrSearchResultResponse.cs`, `ClassSearchResultResponse.cs`; `ETR.API/appsettings.json`.
**Mục tiêu:** Xử lý toàn bộ 10 vấn đề (B1→B10) do frontend team báo cáo trong `ETR.Documentation/report_20260803.md`. Đây đều là các gap đã được đối chiếu và ghi nhận trong `LO_TRINH_HOAN_THIEN_DU_AN.md` dưới mã H14/H15/H6(mở rộng)/H16/H17/H18/H19/M16/M17, cộng với phần còn lại của H1 (bug AND-combination trên `EtrController`, đã sửa một phần cho `ApprovalsController` ở batch trước 2026-08-03).

---

## 1. Tóm tắt những gì đã implement — theo từng vấn đề FE báo cáo

### B1 — Academic/TrainingManager bị 403 khi thao tác với ETR

**Nguyên nhân gốc:** ASP.NET Core kết hợp `[Authorize(Roles=...)]` ở class-level và method-level theo **AND**, không phải OR. `EtrController` có class-level `[Authorize(Roles = "Admin,QA,Student,Instructor,Audit")]` — thiếu `Academic` và `TrainingManager` — nên dù một action bên trong có method-level cho phép 2 role này, request vẫn bị chặn ngay từ class-level.

**Đã sửa:**
- Bỏ hẳn role-list ở class-level, chỉ giữ `[Authorize]` (xác thực đăng nhập) — để mỗi action tự quyết định role qua method-level attribute của chính nó, không còn lớp AND ẩn phía trên.
- `Submit` (`POST /api/etr/{id}/submit`): thêm `Academic` vào danh sách role (trước: `Instructor,Admin`; sau: `Instructor,Admin,Academic`).
- `GetAllEtrs` (`GET /api/etr`) và `GetEtrById` (`GET /api/etr/{id}`): phát hiện thêm khi test sống — `TrainingManager` chưa từng được cấp ở method-level cho 2 action này (không chỉ là bug AND). Đã thêm `TrainingManager` vào cả hai.

**Trước/sau cho FE:**
| Endpoint | Trước | Sau |
|---|---|---|
| `GET /api/etr` | 403 với Academic, TrainingManager | 200 |
| `GET /api/etr/{id}` | 403 với Academic, TrainingManager | 200 |
| `POST /api/etr/{id}/submit` | 403 với Academic | 200 (nếu qua được business rule) |

### B2 — Instructor không xem được danh sách Class/Course/Subject

Class-level `[Authorize(Roles = "Admin,Academic,TrainingManager")]` trên cả 3 controller thiếu `Instructor`.
**Đã sửa:** thêm `Instructor` vào class-level của `ClassesController`, `CoursesController`, `SubjectsController`. Để không vô tình mở quyền ghi, đã thêm `[Authorize(Roles = "Admin,Academic,TrainingManager")]` (không có Instructor) tường minh trên từng action `Create`/`Update`/`Delete` — các action ghi vẫn giữ nguyên phạm vi cũ.

**Kết quả:** Instructor gọi `GET /api/classes`, `GET /api/courses`, `GET /api/subjects` → 200 (trước: 403). Instructor gọi `POST/PUT/DELETE` trên 3 endpoint này vẫn 403 như cũ (không đổi).

### B3 — QA/Instructor/Audit không xem được Enrollments

`EnrollmentsController` class-level chỉ có `Admin,Academic`.
**Đã sửa:** class-level → `Admin,Academic,QA,Instructor,Audit`; thêm `[Authorize(Roles = "Admin,Academic")]` tường minh trên `Create`/`Update`/`Delete` để giữ nguyên phạm vi ghi.

**Kết quả:** QA, Instructor, Audit gọi `GET /api/enrollments`, `GET /api/enrollments/{id}`, `GET /api/enrollments/student/{studentId}` → 200 (trước: 403).

### B4 — QA/Audit/Instructor không xem được User Profiles

`UserProfilesController` không có bug AND (class-level vốn đã là `[Authorize]` trần), chỉ là method-level role-list thiếu.
**Đã sửa 3 action:**
- `GetAllUserProfiles` (`GET /api/userprofiles`): thêm `QA,Audit`.
- `GetLearnerProfiles` (`GET /api/userprofiles/learners`): thêm `QA,Audit`.
- `GetUserProfileByAccountId` (`GET /api/userprofiles/{accountId}`): thêm `QA,Audit,Instructor`.

**Cập nhật sau review pass (2026-08-04):** merge từ `main` chỉ thêm Instructor vào endpoint tra-theo-ID, KHÔNG thêm vào `GetAllUserProfiles`/`GetLearnerProfiles` — không khớp đúng yêu cầu B4 (FE báo thiếu Instructor ở cả 3 endpoint, không phân biệt). `/mpower:flow-review-code` phát hiện gap này và đã bổ sung `Instructor` vào cả 2 endpoint enumeration còn lại để khớp đúng B4. Chấp nhận rủi ro over-provisioning (Instructor giờ xem được toàn bộ profile hệ thống, không chỉ học viên lớp mình) như một đánh đổi tạm thời — theo dõi ở mục **H20** trong roadmap (gộp với H6) cho tới khi có logic scope "chỉ học viên lớp mình phụ trách" dựa trên `Class.InstructorAccountId`.

### B5 — QA/Academic không xem được Audit Trail

`AuditController` class-level chỉ có `Admin,Audit`.
**Đã sửa:** class-level → `Admin,Audit,QA,Academic`. Toàn bộ action trong controller này chỉ đọc (không có action ghi) nên không cần narrow lại action nào.

### B6 — Instructor không xem được Evidence Types

`EvidenceTypesController` class-level chỉ có `Admin,Academic`.
**Đã sửa:** class-level → `Admin,Academic,Instructor`; thêm `[Authorize(Roles = "Admin,Academic")]` tường minh trên `Create`/`Update`/`Delete` để giữ nguyên phạm vi ghi.

### B7 — Thiếu endpoint cập nhật Department cho Account

Trước đây `AccountsController` chỉ có `PUT /api/accounts/{id}/status` và `PUT /api/accounts/{id}/role`, không có endpoint đổi phòng ban.

**Đã thêm endpoint mới**, theo đúng pattern của 2 endpoint kia (kể cả ghi `AuditLog`):

```
PUT /api/accounts/{id}/department
Authorization: Admin
Body: { "departmentId": 1 }
→ 204 No Content
```

Trả `404` nếu `id` account không tồn tại.

**Cập nhật sau review pass (2026-08-04):** ban đầu `departmentId` không tồn tại chỉ được chặn gián tiếp qua FK constraint ở DB (lộ `DbUpdateException` thô ra client). Đã thêm kiểm tra `DepartmentRepository.GetByIdAsync(departmentId)` tường minh trước khi ghi — `departmentId` không tồn tại nay trả `400 Business rule violation` với message rõ ràng thay vì lỗi DB thô.

### B8 — Academic không upload/xoá được Evidence

`UploadEvidence` và `DeleteEvidence` trong `EvidencesController` trước chỉ cho `Instructor,Admin`.
**Đã sửa:** cả 2 action → `Instructor,Admin,Academic`. (Action `VerifyEvidence` — `QA,Admin` — giữ nguyên, đây là ranh giới segregation-of-duties đã cố ý thiết lập ở batch trước, Academic không được verify).

**Cập nhật sau review pass (2026-08-04):** phát hiện 2 gap liên quan trực tiếp tới B8 mà bản merge ban đầu bỏ sót:
1. `GetAll` (`GET /api/evidences`, danh sách minh chứng) vẫn chỉ cho `Instructor,QA,Admin` — Academic upload/xoá được nhưng không liệt kê được minh chứng của chính mình. Đã thêm `Academic` vào role list.
2. `EvidenceService.DeleteEvidenceAsync` không ghi `AuditLog` — giờ Academic (thêm vào batch này) cũng xoá được, khoảng trống audit trail này trở thành rủi ro thực sự cho kiểm soát segregation-of-duties. Đã thêm `AuditLog` (ActionType="DELETE") theo đúng pattern curated-log đã dùng ở `AccountService`.

### B9 — `SubjectResultResponse` thiếu field `Score`

DTO trước chỉ có `AttendanceRate`, thiếu điểm số (`Score`) mà entity `SubjectResult` đã có sẵn.
**Đã sửa:** thêm field `Score` (decimal?, nullable) vào `SubjectResultResponse`, map trong `EtrService.GetEtrByIdAsync`.

```json
// GET /api/etr/{id} → subjectResults[]
{
  "subjectResultId": 1,
  "subjectId": 1,
  "status": "Passed",
  "createdAt": "...",
  "attendanceRate": 100.0,
  "score": 85.0   // field mới
}
```

### B10 — Search trả raw entity thay vì dữ liệu đã làm giàu

`GET /api/search/classes` và `GET /api/search/etrs` trước trả thẳng entity `Class`/`ETRCourseRecord` (chỉ có ID, không có tên hiển thị).
**Đã sửa:** tạo 2 DTO mới, join dữ liệu sẵn có (Class↔Course, ETR↔Enrollment↔UserProfile/Class/Course) để trả về tên hiển thị thay vì chỉ ID:

```json
// GET /api/search/classes?query=...
[{
  "classId": 1, "classCode": "AMT101-C1", "className": "AMT-101 Batch 1",
  "courseCode": "AMT-101", "courseName": "Aircraft Maintenance Technician - Basic",
  "status": "Completed"
}]

// GET /api/search/etrs?query=...
[{
  "etrCourseRecordId": 1, "status": "Completed", "studentName": "Jane Student",
  "classCode": "AMT101-C1", "className": "AMT-101 Batch 1",
  "courseCode": "AMT-101", "courseName": "Aircraft Maintenance Technician - Basic"
}]
```

Lưu ý: `query` vẫn là tham số bắt buộc (không đổi) — gọi thiếu `query` sẽ trả `400` như trước, đây là hành vi model-validation sẵn có, không phải lỗi.

---

## 2. Đã kiểm chứng bằng cách nào

- `dotnet build` toàn solution: **0 Error** (4 warning có sẵn từ trước, không liên quan).
- `dotnet test` (`ETR.Application.Tests`): **23/23 pass**, không có regression.
- Chạy app thật (Docker SQL Server dev), đăng nhập đủ 7 role (Admin/Instructor/QA/Academic/TrainingManager/Student/Audit), `curl` trực tiếp từng endpoint:
  - `GET /api/etr` với TrainingManager: 403 → **200**.
  - `GET /api/etr` với Academic: **200** (cả 2 ETR seed data).
  - `POST /api/etr/1/submit` với Academic: **400** business-rule ("1 evidence file(s) are not yet Verified") — xác nhận đã qua được role-gate, chỉ còn chặn bởi business rule hợp lệ, không phải 403.
  - `GET /api/classes|courses|subjects` với Instructor: **200** cho cả 3.
  - `GET /api/enrollments` với QA/Instructor/Audit: **200** cho cả 3.
  - `GET /api/userprofiles` với QA/Audit: **200**.
  - `GET /api/audit` với QA/Academic: **200**.
  - `GET /api/evidencetypes` với Instructor: **200**.
  - `PUT /api/accounts/3/department` với Admin, body hợp lệ: **204**.
  - `DELETE /api/evidences/9999` với Academic (ID không tồn tại, cố ý để kiểm tra role-gate mà không cần dàn dựng đủ điều kiện nghiệp vụ): **404** — xác nhận qua được role-gate (không phải 403), chỉ dừng ở "not found" như kỳ vọng.
  - `GET /api/etr/1` với Admin: response chứa field `score` trong từng `subjectResults[]`.
  - `GET /api/search/etrs?query=Completed`, `GET /api/search/classes?query=a`: cả 2 trả về DTO đã làm giàu với tên học viên/lớp/khoá học, không còn raw entity.

## 2b. Review pass sau merge (2026-08-04) — `/mpower:flow-review-code`

Sau khi batch fix ở trên hoàn tất, một batch fix độc lập từ `main` (cùng xử lý report_20260803.md, do một dev khác thực hiện song song) được merge vào — conflict được resolve thủ công (xem review report để biết chi tiết resolve). Để xác nhận merge không làm hỏng gì và cả 10 mục B1-B10 vẫn đúng sau merge, đã chạy `/mpower:flow-review-code` (code review + security review song song, ASVS L2) trên toàn bộ diff `ec89bc9..HEAD` (20 file). Report đầy đủ: `.claude/workspace/reviews/flow-review-2026-08-04-files-2a2fc868.md`.

**Kết quả đối chiếu B1-B10 sau merge:** 7/10 đúng nguyên trạng, 3/10 (B4, B7, B8) có gap phát sinh trong quá trình merge/hoàn thiện — đã fix ngay trong lượt review này (xem lại chi tiết ở mục B4/B7/B8 phía trên). Merge tự nó không làm mất nội dung hợp lệ của bên nào — 2 review agent độc lập cùng xác nhận.

**Thêm 1 fix ngoài phạm vi B-item nhưng cần thiết:** `appsettings.json` — `DefaultConnection` password bị đổi thành `.` (1 ký tự) trong lúc merge, một artefact từ máy dev khác, làm hỏng `dotnet run` cho bất kỳ ai dùng đúng convention Docker SQL Server sẵn có của dự án. Đã khôi phục lại `MyStrongPassword123`.

**Việc phát hiện nhưng CHƯA fix (ghi nhận minh bạch, theo dõi ở roadmap):**
- **H20**: Instructor giờ có full-visibility (không scope) trên cả 3 endpoint `UserProfiles` — rủi ro IDOR đã biết, cần logic scope "chỉ học viên lớp mình phụ trách".
- **H21**: cùng loại rủi ro (unscoped enumeration bởi Instructor/QA/Audit) trên `EnrollmentsController` và `EtrController` (student-history/current-status) — gộp chung với H20 vì cùng nguyên nhân gốc, nên sửa 1 lần.
- **H22**: `AccountsController.GetAccountById` mở rộng cho 6 role lộ `Username` (email) — cần xác nhận với tác giả thay đổi (từ `main`) trước khi thu hẹp lại.
- Ghi chú riêng: việc mở rộng `AuditController` cho Academic/QA (B5) bị security review gắn cờ là rủi ro segregation-of-duties, nhưng đây là yêu cầu nghiệp vụ đã có trong báo cáo FE (B5) và roadmap (H16) — KHÔNG revert, chỉ ghi nhận làm điểm cần xác nhận lại nếu tham chiếu FRD không đúng.

## 3. Rủi ro/việc còn lại

- **H1 nay đã đóng hoàn toàn** cho cả `ApprovalsController` (batch 2026-08-03) và `EtrController` (batch này) — không còn controller nào trong hệ thống có class-level role-list hẹp hơn method-level.
- **H6/H20/H21 (còn mở, gộp chung)**: Instructor/QA/Audit hiện có full-visibility (không scope theo "lớp/học viên mình phụ trách") trên `UserProfiles`, `Enrollments`, và ETR student-history — cần 1 lượt scoping riêng dựa trên `Class.InstructorAccountId` (đã có từ batch C8).
- **H22 (mới)**: `AccountsController.GetAccountById` mở rộng cho 6 role, cần xác nhận phạm vi đúng với tác giả gốc.
- B1-B10 không đụng tới các mục ngoài phạm vi báo cáo FE (C1/C9/C10 mock-password/secrets/mock-admin-token, H2/H3/H5/H7-H13) — giữ đúng theo yêu cầu chỉ ưu tiên 10 vấn đề FE báo cáo.

## 4. Files liên quan

| File | Trạng thái |
|---|---|
| `ETR.API/Controllers/EtrController.cs` | Sửa — bỏ role-list class-level, thêm Academic (Submit) + TrainingManager (GetAllEtrs, GetEtrById) |
| `ETR.API/Controllers/ClassesController.cs` | Sửa — class-level thêm Instructor, narrow lại Create/Update/Delete |
| `ETR.API/Controllers/CoursesController.cs` | Sửa — tương tự Classes |
| `ETR.API/Controllers/SubjectsController.cs` | Sửa — tương tự Classes |
| `ETR.API/Controllers/EnrollmentsController.cs` | Sửa — class-level thêm QA,Instructor,Audit, narrow lại Create/Update/Delete |
| `ETR.API/Controllers/UserProfilesController.cs` | Sửa — 3 action thêm QA/Audit/Instructor (review pass: thêm Instructor vào GetAll + learners để khớp B4) |
| `ETR.API/Controllers/AuditController.cs` | Sửa — class-level thêm QA,Academic |
| `ETR.API/Controllers/EvidenceTypesController.cs` | Sửa — class-level thêm Instructor, narrow lại Create/Update/Delete |
| `ETR.API/Controllers/EvidencesController.cs` | Sửa — Upload/Delete thêm Academic (review pass: thêm Academic vào GetAll) |
| `ETR.API/Controllers/AccountsController.cs` | Sửa — endpoint mới `PUT {id}/department` |
| `ETR.Application/Services/AccountService.cs` | Sửa — thêm `UpdateAccountDepartmentAsync` (review pass: thêm kiểm tra `departmentId` tồn tại) |
| `ETR.Application/Services/EvidenceService.cs` | Sửa (review pass) — thêm `AuditLog` cho `DeleteEvidenceAsync` |
| `ETR.Application/Interfaces/IAccountService.cs` | Sửa — thêm khai báo tương ứng |
| `ETR.Application/DTOs/Account/AccountDtos.cs` | Sửa — thêm `UpdateAccountDepartmentRequest` |
| `ETR.Application/DTOs/Etr/Responses/SubjectResultResponse.cs` | Sửa — thêm field `Score` |
| `ETR.Application/Services/EtrService.cs` | Sửa — map field `Score` |
| `ETR.Application/DTOs/Search/EtrSearchResultResponse.cs` | Mới |
| `ETR.Application/DTOs/Search/ClassSearchResultResponse.cs` | Mới |
| `ETR.API/Controllers/SearchController.cs` | Sửa — trả DTO làm giàu thay vì raw entity |
| `ETR.API/appsettings.json` | Sửa (review pass) — khôi phục password dev bị đổi trong lúc merge |
| `ETR.Documentation/LO_TRINH_HOAN_THIEN_DU_AN.md` | Sửa — đánh dấu H14-H19, M16-M17, H1 là đã sửa |
