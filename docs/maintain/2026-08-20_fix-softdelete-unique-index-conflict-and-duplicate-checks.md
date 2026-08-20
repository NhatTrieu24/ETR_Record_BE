# Fix Soft-Delete / Unique-Index Conflict (seed "Training" duplicate key) + Bổ sung Duplicate-Check nghiệp vụ toàn hệ thống — 2026-08-20

**Ngày thực hiện:** 2026-08-20
**Phạm vi:** `ETR.Infrastructure/Data/AppDbContext.cs`, `ETR.Infrastructure/Data/DataSeeder.cs`, migration mới `20260820101945_FilterUniqueIndexesBySoftDelete`; 9 service trong `ETR.Application/Services/` (Department, EvidenceType, Course, Subject, Class, Account, UserProfile, Attendance, PracticalChecklistResult).
**Mục tiêu:** Sửa lỗi crash khi khởi động app (`Cannot insert duplicate key row … (Training)`), tìm và xử lý toàn bộ các entity khác có cùng dạng xung đột giữa soft-delete filter và unique index ở tầng DB, đồng thời bổ sung validation duplicate-check ở tầng nghiệp vụ (thay vì để SQL ném lỗi thô).

---

## 1. Tóm tắt những gì đã implement

### 1.1 Root cause của lỗi crash khi seed "Training"

`AppDbContext.ConfigureSoftDeleteFilters()` áp dụng global query filter `IsDeleted == false` cho MỌI entity kế thừa `BaseEntity`. Trong khi đó, unique index `IX_Departments_DepartmentName` (và 14 unique index khác cùng dạng) là **plain unique index**, không có `HasFilter`, nên nó chặn trùng khoá trên **toàn bộ dòng dữ liệu bất kể `IsDeleted`**.

Chuỗi sự kiện gây crash: có 1 dòng `Department` tên "Training" đã bị soft-delete (`IsDeleted = true`) từ trước → check `AnyAsync` trong `DataSeeder.SeedIdentityAsync` bị global filter loại dòng đó ra khỏi kết quả → trả về `false` → seed cố insert dòng "Training" mới → SQL Server unique index (không biết gì về `IsDeleted`) từ chối vì đã có key trùng → `SqlException` 2601 văng thẳng ra ngoài, app crash khi khởi động.

**Fix:** thêm `.IgnoreQueryFilters()` vào check tồn tại trong `DataSeeder.cs` — seed giờ nhìn thấy cả dòng soft-delete khi kiểm tra trùng tên, không insert lại nữa.

### 1.2 Bug không liên quan phát hiện kèm theo — lỗi gõ chặn build

Trong lúc verify, phát hiện `AppDbContext.cs` có 1 dòng gọi `CreateIsDeleđoạntedFilter(...)` (tên method bị gõ nhầm, lẫn ký tự tiếng Việt vào giữa `CreateIsDeletedFilter`) — lỗi này có sẵn trong working tree (chưa commit), khiến **toàn bộ solution không build được**, độc lập với bug seed. Đã sửa lại đúng tên method thật.

### 1.3 Rà soát toàn hệ thống — 12 entity khác cũng có cùng xung đột

Sau khi fix bug gốc, rà soát toàn bộ `HasIndex(...).IsUnique()` trong `AppDbContext.cs` đối chiếu với entity nào kế thừa `BaseEntity` (có `IsDeleted`). Kết quả: **12/12 entity có unique index đều kế thừa `BaseEntity`**, tức đều có nguy cơ gặp đúng lỗi này — không chỉ riêng `Department`:

| Entity | Unique field(s) |
|---|---|
| Role | RoleName |
| Department | DepartmentName |
| EvidenceType | TypeName |
| Course | CourseCode |
| Subject | SubjectCode |
| Class | ClassCode |
| Account | Username |
| UserProfile | Email (đã có filter `NOT NULL AND <> ''` từ trước, chưa có `IsDeleted`) |
| CourseEnrollment | AccountId + ClassId |
| ETRCourseRecord | EnrollmentId |
| SubjectResult | EtrId + CourseId + SubjectId |
| ClassSubject | ClassId + SubjectId |
| AttendanceRecord | SessionId + EnrollmentId |
| AssessmentResult | AssessmentId + AccountId + SessionId + AttemptNo (đã có filter `SessionId IS NOT NULL`, chưa có `IsDeleted`) |
| PracticalChecklistResult | SubjectResultId + PracticalChecklistId |

Phát hiện đáng chú ý nhất: `ClassService.UpdateClassAsync` soft-delete toàn bộ `ClassSubject` cũ của lớp rồi **insert lại ngay trong cùng request** với đúng `(ClassId, SubjectId)` — đây không phải edge case lý thuyết, mà là **đường đi bình thường mỗi lần update lớp có gán môn học**, lẽ ra phải luôn văng lỗi trùng khoá trước khi có fix này.

### 1.4 Quyết định nghiệp vụ: cho phép tái sử dụng key đã soft-delete

Đã xác nhận với user (AskUserQuestion): khi check trùng trước Create/Update, **chỉ coi là trùng nếu dòng đang active (`IsDeleted == false`)** — cho phép tạo mới trùng tên/mã với một dòng đã bị xoá trước đó (ví dụ: xoá Department "Training" rồi tạo lại Department "Training" mới phải được phép).

Để quyết định này có hiệu lực thật (không bị SQL chặn ngầm), đã đổi cả 15 unique index sang **filtered index** `WHERE [IsDeleted] = 0` (migration `20260820101945_FilterUniqueIndexesBySoftDelete`) — unique constraint giờ chỉ tính trên dòng active, khớp đúng với check ở tầng ứng dụng (vốn đã tự động lọc `IsDeleted` nhờ global query filter, không cần đổi gì thêm ở logic check).

### 1.5 Bug migration có sẵn phát hiện kèm theo — index `AssessmentResults` chưa từng migrate đúng

Khi generate migration cho mục 1.4, `dotnet ef database update` báo lỗi index không tồn tại: model đã khai báo unique index trên `AssessmentResults` gồm 4 cột (`AssessmentId, AccountId, SessionId, AttemptNo`), nhưng **chưa từng có migration nào áp dụng cột `AttemptNo` vào index này** — DB thật vẫn chỉ có index 3 cột gốc từ migration `CleanBaseData`. Ai đó đã sửa Fluent API config nhưng quên chạy `dotnet ef migrations add`. Đã sửa tay migration để DROP đúng tên index 3 cột thật trong DB và tạo lại đúng index 4 cột + filter `IsDeleted = 0`, gộp luôn migration bị thiếu vào lần này.

### 1.6 Bổ sung duplicate-check nghiệp vụ ở tầng Service

Trước đây phần lớn Create/Update chỉ dựa vào SQL ném lỗi thô (`SqlException` 2601) khi trùng khoá — trải nghiệm xấu, lộ chi tiết kỹ thuật. Đã bổ sung check tường minh (dùng `BusinessRuleViolationException`, map sẵn về HTTP 400 qua `GlobalExceptionHandler`) cho:

| Service | Field check | Create | Update |
|---|---|---|---|
| DepartmentService | DepartmentName | ✅ | ✅ (loại trừ chính nó) |
| EvidenceTypeService | TypeName | ✅ | ✅ |
| CourseService | CourseCode | ✅ | ✅ |
| SubjectService | SubjectCode | ✅ | ✅ |
| ClassService | ClassCode | ✅ | ✅ |
| AccountService | Username | ✅ | N/A (không có luồng đổi Username) |
| UserProfileService | Email (chỉ khi không rỗng) | ✅ | ✅ |
| AttendanceService | SessionId + EnrollmentId | ✅ (RecordAttendanceAsync) | N/A |
| PracticalChecklistResultService | SubjectResultId + PracticalChecklistId | ✅ | N/A |

`ClassSubject` (bug 1.3) và `CourseEnrollment` không cần sửa thêm code — filtered index ở mục 1.4 tự giải quyết, và `EnrollmentService` đã có sẵn business logic riêng dùng `EnrollmentStatus` enum (không dựa vào `IsDeleted`).

---

## 2. Đã kiểm chứng bằng cách nào

- `dotnet build` (toàn solution): **0 Error** sau mỗi bước sửa (typo fix, service edits, migration).
- `dotnet ef database update`: migration `20260820101945_FilterUniqueIndexesBySoftDelete` áp dụng thành công lên DB thật (Azure SQL) — xác nhận qua log SQL: 15 `DROP INDEX` + 15 `CREATE UNIQUE INDEX ... WHERE [IsDeleted] = 0` (kèm điều kiện gốc cho `UserProfiles.Email` và `AssessmentResults`), insert `__EFMigrationsHistory` thành công.
- Chạy app thật 2 lần (`dotnet run`, ~15s mỗi lần — trước và sau migration): log khởi động sạch, không còn `fail`/`duplicate`/`exception` nào; xác nhận thêm bằng cách so sánh SQL log của seed check trên bảng `Departments` — trước fix có `WHERE [IsDeleted] = 0` trong query (gây bug), sau fix dùng `IgnoreQueryFilters()` không còn mệnh đề đó.

## 3. Rủi ro/việc còn lại

- Chưa viết integration test cho các duplicate-check mới (9 service) — hiện chỉ verify bằng build + chạy app thật, chưa có test tự động cho case "tạo trùng tên bị chặn 400" hay "tạo trùng tên đã xoá thì được phép".
- `ETRCourseRecord` và `SubjectResult` (unique index cũng đã đổi sang filtered) chưa có duplicate-check tường minh ở service layer — rủi ro thấp vì các FK liên quan luôn là giá trị mới tự sinh trong cùng transaction, nhưng nếu sau này có luồng "tạo lại"/retry thủ công thì nên bổ sung.
- Migration lịch sử của `AssessmentResults` (mục 1.5) cho thấy quy trình review Fluent API change hiện tại không bắt buộc kiểm tra migration đã áp dụng thật hay chưa — đã ghi thành recommendation (`.claude/workspace/recommendation/2026-08-20-ef-migration-model-drift-before-index-changes.md`) để lưu ý cho các thay đổi index/schema sau này.

## 4. Files liên quan

| File | Trạng thái |
|---|---|
| `ETR.Infrastructure/Migrations/20260820101945_FilterUniqueIndexesBySoftDelete.cs`, `.Designer.cs` | Mới |
| `ETR.Infrastructure/Data/AppDbContext.cs` | Sửa — fix lỗi gõ `CreateIsDeleđoạntedFilter`; thêm `.HasFilter("[IsDeleted] = 0")` cho 15 unique index; gộp fix index `AssessmentResults` (4 cột) chưa từng migrate |
| `ETR.Infrastructure/Migrations/AppDbContextModelSnapshot.cs` | Sửa — cập nhật snapshot khớp migration mới |
| `ETR.Infrastructure/Data/DataSeeder.cs` | Sửa — `.IgnoreQueryFilters()` khi check trùng Department trong seed |
| `ETR.Application/Services/DepartmentService.cs` | Sửa — thêm duplicate-check `DepartmentName` ở Create/Update |
| `ETR.Application/Services/EvidenceTypeService.cs` | Sửa — thêm duplicate-check `TypeName` ở Create/Update |
| `ETR.Application/Services/CourseService.cs` | Sửa — thêm duplicate-check `CourseCode` ở Create/Update |
| `ETR.Application/Services/SubjectService.cs` | Sửa — thêm duplicate-check `SubjectCode` ở Create/Update |
| `ETR.Application/Services/ClassService.cs` | Sửa — thêm duplicate-check `ClassCode` ở Create/Update |
| `ETR.Application/Services/AccountService.cs` | Sửa — thêm duplicate-check `Username` ở Create |
| `ETR.Application/Services/UserProfileService.cs` | Sửa — thêm duplicate-check `Email` ở Create/Update (bỏ qua khi rỗng) |
| `ETR.Application/Services/AttendanceService.cs` | Sửa — thêm duplicate-check `SessionId + EnrollmentId` ở `RecordAttendanceAsync` |
| `ETR.Application/Services/PracticalChecklistResultService.cs` | Sửa — thêm duplicate-check `SubjectResultId + PracticalChecklistId` ở Create |
| `.claude/workspace/recommendation/2026-08-20-ef-migration-model-drift-before-index-changes.md` | Mới — ghi nhận bài học về migration bị thiếu (mục 1.5) |
