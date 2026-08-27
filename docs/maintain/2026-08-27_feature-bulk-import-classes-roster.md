# Bulk Import Lớp học + Danh sách học viên qua Excel (Academic) — 2026-08-27

**Ngày thực hiện:** 2026-08-27
**Phạm vi:** `ETR.API/Controllers/ImportController.cs`; `ETR.Application/Services/ImportService.cs` + `ETR.Application/Interfaces/IImportService.cs`; `ETR.Application/DTOs/Import/ClassImportRow.cs`, `StudentRosterImportRow.cs` (mới); `ETR.Application/Services/ClassService.cs` + `IClassService.cs`; `ETR.Application/Services/EnrollmentService.cs` + `IEnrollmentService.cs`; `ETR.Domain/Enums/AuditActionType.cs`.
**Mục tiêu:** Theo yêu cầu `/mpower:code-fix` + `/mpower:code-refactor`: thay thế luồng thủ công "tạo từng lớp rồi ghi danh từng học viên một" (role Academic phải làm tay từng học viên) bằng một API bulk import qua file Excel, tạo hàng loạt lớp học và ghi danh hàng loạt học viên vào đúng lớp trong một lần upload, có validate đầy đủ theo đúng nghiệp vụ hiện có của hệ thống.

---

## 1. Tóm tắt những gì đã implement

### 1.1 Quyết định thiết kế (đã xác nhận với user trước khi code)

Vì đây là tính năng hoàn toàn mới, không có tiền lệ trong code, 3 quyết định nghiệp vụ sau đã được chốt trực tiếp với user trước khi implement:

1. **Học viên trong file phải là tài khoản Student đã tồn tại sẵn** — API này không tự tạo Account/UserProfile mới. Nếu Academic có học viên hoàn toàn mới, phải chạy `POST /api/import/accounts/*` (đã có sẵn từ batch 2026-08-18) để tạo tài khoản trước, rồi mới dùng API này để ghi danh vào lớp. Lý do: giữ đúng ranh giới trách nhiệm rõ ràng giữa "tạo tài khoản" và "ghi danh vào lớp" — hai nghiệp vụ độc lập, tránh API này phình to và lặp lại logic tạo tài khoản đã có.
2. **1 file Excel, 2 sheet** (`Classes` + `Students`) thay vì 2 endpoint riêng — sheet `Students` tham chiếu `ClassCode` từ sheet `Classes` (hoặc một lớp đã tồn tại sẵn trong hệ thống), validate được tính nhất quán giữa 2 sheet trong cùng một lượt Validate/Commit.
3. **All-or-nothing toàn file** — nếu bất kỳ dòng nào (ở cả 2 sheet) lỗi, không tạo gì cả. Nhất quán với pattern Attendance/Account import đã có.

### 1.2 Endpoint mới

| Method | Endpoint | Auth |
|---|---|---|
| GET | `/api/import/classes-roster/template` | Admin, Academic |
| POST multipart | `/api/import/classes-roster/validate` | Admin, Academic |
| POST multipart | `/api/import/classes-roster/commit` | Admin, Academic |

Tái sử dụng đúng pattern 2-bước **Validate → Commit** đã có sẵn cho Attendance/Assessment/Accounts (`docs/maintain/2026-08-11_feature-bulk-import-excel.md`, `docs/maintain/2026-08-18_fix-error-handling-enum-hoa-status-va-bulk-import-account.md`).

**Sheet `Classes`** (tạo lớp mới): `ClassCode*`, `ClassName*`, `CourseCode*` (dropdown lấy từ Course hiện có), `Ngày bắt đầu*`/`Ngày kết thúc*` (dd/MM/yyyy), `Địa điểm`, `Sĩ số tối đa*`, `Trạng thái*` (dropdown `ClassStatus`).

**Sheet `Students`** (ghi danh vào lớp): `ClassCode*` (tham chiếu lớp ở sheet `Classes` hoặc lớp đã có sẵn trong hệ thống), `Username học viên (email)*`.

### 1.3 Validate — đúng theo nghiệp vụ hiện có (mục 1.2 của yêu cầu)

**Sheet Classes** (mirror nguyên các rule của `ClassService.CreateClassAsync`):
- `ClassCode` không được trùng trong file, không được trùng lớp đã tồn tại trong hệ thống.
- `CourseCode` phải tồn tại (Course chưa bị xóa).
- `StartDate`/`EndDate` phải parse được (chấp nhận cả kiểu Excel date thật lẫn text `dd/MM/yyyy`); `EndDate >= StartDate`.
- `Capacity >= 1`.
- `Status` phải là 1 trong các giá trị hợp lệ của `ClassStatus`.

**Sheet Students** (mirror nguyên các rule của `EnrollmentService.CreateEnrollmentAsync`):
- `ClassCode` phải trỏ được đến một lớp — hoặc trong sheet `Classes` của cùng file, hoặc một lớp đã tồn tại trong hệ thống.
- `Username` phải là tài khoản đã tồn tại, đúng role **Student**, và đã có `UserProfile` (tài khoản tạo qua bulk Account import mà chưa gắn `UserProfile` sẽ bị chặn ở đây với thông báo rõ ràng, thay vì để lỗi 500 mơ hồ lúc ghi danh).
- Không ghi danh trùng vào cùng 1 lớp (nếu lớp đã tồn tại sẵn trong hệ thống).
- Không ghi danh nếu học viên đang có ETR chưa khóa (`IsLocked = false`) ở một lớp khác cùng khóa học (đúng rule "1 khóa học chỉ được có 1 ETR đang chạy tại một thời điểm").
- Khóa học của lớp phải có ít nhất 1 `CourseSubject` được cấu hình.
- Không trùng lặp cặp (ClassCode, Username) trong cùng file.

Lỗi trả về gắn kèm tên sheet (`Classes.ClassCode`, `Students.Username`, …) để phân biệt vì hai sheet có thể trùng số dòng.

### 1.4 Commit — tái sử dụng logic nghiệp vụ đã có, không viết lại

Thay vì viết lại logic tạo lớp + ghi danh (bao gồm cả cơ chế phức tạp "retake chỉ học lại môn chưa Pass" khi học viên từng học khóa này ở một ETR trước đó đã khóa) ngay trong `ImportService`, đã **refactor** để tái sử dụng chính xác logic đã được kiểm chứng:

- `ClassService.CreateClassAsync` được tách thành `CreateClassCoreAsync` (không tự mở/đóng transaction) + một wrapper mỏng `CreateClassAsync` mở transaction rồi gọi `CreateClassCoreAsync`. Hành vi API `POST /api/classes` cũ **không đổi**.
- `EnrollmentService.CreateEnrollmentAsync` được tách tương tự thành `CreateEnrollmentCoreAsync` + wrapper. Hành vi API `POST /api/enrollments` cũ **không đổi**.
- `ImportService.CommitClassRosterImportAsync` mở **một transaction duy nhất** cho toàn bộ file, gọi `_classService.CreateClassCoreAsync(...)` cho từng dòng sheet `Classes`, rồi `_enrollmentService.CreateEnrollmentCoreAsync(...)` cho từng dòng sheet `Students`, cuối cùng ghi 1 `AuditLog` (`IMPORT_CLASS_ROSTER` — giá trị mới thêm vào `AuditActionType`) rồi commit. Nếu bất kỳ dòng nào ném exception, toàn bộ transaction rollback — đúng all-or-nothing đã chốt ở mục 1.1.

**Lý do refactor thay vì viết logic riêng:** `CreateEnrollmentAsync` gốc có ~120 dòng logic tạo `ETRCourseRecord` + `SubjectResult` + `AssessmentResult`/`PracticalChecklistResult` cho từng môn, bao gồm cả cơ chế carry-over điểm khi học viên retake. Viết lại logic này trong `ImportService` sẽ tạo ra 2 cài đặt độc lập của cùng một nghiệp vụ — rủi ro drift/bug khi một bên được sửa mà bên kia quên cập nhật. Tách `...CoreAsync` giữ đúng nguyên tắc DRY, một nguồn sự thật duy nhất cho "ghi danh học viên vào lớp", dùng chung bởi cả API đơn lẻ lẫn bulk import.

---

## 2. Đã kiểm chứng bằng cách nào

- `dotnet build ETRSystem.slnx`: **0 Error** trên cả 5 project sau refactor `ClassService`/`EnrollmentService` và thêm toàn bộ code mới.
- `dotnet test ETR.Application.Tests`: **19/21 pass** — 2 test fail (`AccountServiceTests.CreateAccountAsync_*`) đã xác nhận là **lỗi có sẵn từ trước, không liên quan đến thay đổi lần này** (chạy lại y hệt trên `git stash` — tức là trạng thái code trước khi có bất kỳ thay đổi nào của batch này — cho kết quả fail giống hệt).
- **Chưa verify bằng gọi API thật với file Excel cụ thể trong lượt này** — DB cấu hình trong `appsettings.json` là một Azure SQL Database dùng chung cho cả team, nên không tự ý tạo dữ liệu test (Account/Class/Enrollment) trên đó mà không có xác nhận. Đã review code kỹ theo đúng pattern đã verify trước đó cho Attendance/Assessment/Accounts import (cùng cấu trúc Validate/Commit, cùng cơ chế transaction, cùng tác giả).
- Không có unit test riêng cho `ImportService` — nhất quán với hiện trạng: Attendance/Assessment/Accounts import cũng chưa có unit test (không phải khoảng trống riêng của tính năng này).

## 3. Rủi ro/việc còn lại

- **Cần verify bằng file Excel thật trên môi trường test/staging trước khi dùng production** — đặc biệt luồng "học viên retake" (ghi danh vào lớp mới cho khóa học mà học viên từng có ETR đã khóa ở lớp khác) vì đây là nhánh phức tạp nhất của `CreateEnrollmentCoreAsync`.
- **Không hỗ trợ gán Instructor cho từng môn qua Excel** — cố ý bỏ ngoài phạm vi để giữ file đơn giản; `ClassSubject.InstructorAccountId` sẽ để trống, Academic/Admin gán giảng viên sau qua `PUT /api/classes/{id}` như hiện tại.
- **Học viên hoàn toàn mới (chưa có tài khoản) không được hỗ trợ trong 1 lần** — theo đúng quyết định đã chốt ở mục 1.1, cần chạy bulk Account import trước. Nếu sau này cần gộp 2 bước thành 1, cần thêm cột thông tin cá nhân (Họ tên, Password, Phòng ban…) vào sheet `Students` và mở rộng `ValidateStudentRosterRowsAsync`/`CommitClassRosterImportAsync`.
- **All-or-nothing có thể bất tiện với file lớn** (hàng trăm dòng) — nếu sau này cần cho phép "lớp/học viên hợp lệ vẫn được tạo, dòng lỗi bị skip", cần đổi lại logic tương tự khoảng trống đã ghi nhận cho Accounts import (`docs/maintain/2026-08-18_...md`, mục 3).

## 4. Files liên quan

| File | Trạng thái |
|---|---|
| `ETR.Application/DTOs/Import/ClassImportRow.cs`, `StudentRosterImportRow.cs` | Mới |
| `ETR.Domain/Enums/AuditActionType.cs` | Sửa — thêm `IMPORT_CLASS_ROSTER` |
| `ETR.Application/Interfaces/IImportService.cs` | Sửa — thêm 3 method Classes & Roster |
| `ETR.Application/Services/ImportService.cs` | Sửa — thêm toàn bộ logic template/validate/commit cho Classes & Roster; constructor nhận thêm `IClassService`, `IEnrollmentService` |
| `ETR.Application/Interfaces/IClassService.cs`, `Services/ClassService.cs` | Sửa — refactor `CreateClassAsync` thành wrapper mỏng gọi `CreateClassCoreAsync` (method mới, tái sử dụng được, hành vi API cũ không đổi) |
| `ETR.Application/Interfaces/IEnrollmentService.cs`, `Services/EnrollmentService.cs` | Sửa — refactor `CreateEnrollmentAsync` thành wrapper mỏng gọi `CreateEnrollmentCoreAsync` (method mới, tái sử dụng được, hành vi API cũ không đổi) |
| `ETR.API/Controllers/ImportController.cs` | Sửa — thêm 3 endpoint `classes-roster/template`, `classes-roster/validate`, `classes-roster/commit` |
