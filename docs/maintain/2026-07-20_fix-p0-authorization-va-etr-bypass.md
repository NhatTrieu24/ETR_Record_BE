# Fix P0: Authorization theo Role + Vá 3 đường vòng Pre-Validation ETR — 2026-07-20

**Ngày thực hiện:** 2026-07-20
**Phạm vi:** 27 controllers (`ETR.API/Controllers/*.cs`), `ApprovalService.cs`, `EtrService.cs`/`IEtrService.cs`, `AccountService.cs`, `AuthController.cs`, `AppDbContext.Compliance.cs`, `ImmutabilityValidator.cs`, `DataSeeder.cs`
**Mục tiêu:** Vá 8 finding P0 trong `ETR.Documentation/BAO_CAO_REVIEW_HE_THONG.md` (mục "Danh sách công việc ưu tiên", nhóm P0 × Rủi ro Cao) — cụ thể các mục 1, 2, 3, 5, 7, 8, 9, 10. **Không** làm mục 4 (xoá `mock-admin-token`) và mục 6 (xoay JWT/DB secret) — chủ động loại khỏi phạm vi theo yêu cầu.

---

## 1. Tóm tắt những gì đã thay đổi

### 1.1 [Mục 1] Thêm `[Authorize(Roles = "...")]` cho toàn bộ 27 controller

Trước đây `grep -rn "Authorize(Roles" ETR.API/Controllers/*.cs` cho **0 kết quả** — không controller nào giới hạn role thật sự, kể cả 8 controller hoàn toàn không có `[Authorize]`. Đã rà lại từng controller, ưu tiên theo đúng comment XML `[Target Audience]` đã có sẵn; controller nào không có doc thì suy ra role theo nghiệp vụ tương tự (vd. curriculum config dùng chung role với `CoursesController`).

| Controller | Role áp dụng | Ghi chú |
|---|---|---|
| `AccountsController` | `Admin` | |
| `ApprovalsController` | `Admin,Instructor` | Trước đó **không có `[Authorize]`** |
| `AssessmentResultsController` | `Instructor,Admin` (từng action, trừ `GetAssessmentResults`) | `GetAssessmentResults` giữ nguyên bare `[Authorize]` — Student tự xem kết quả của mình, validate trong service |
| `AssessmentsController` | `Admin,Instructor` | |
| `AttendanceController` | `Instructor,Admin` (từng action, trừ `GetAttendanceRecords`) | Cùng pattern với AssessmentResults |
| `AuditController` | `Admin` | Trước đó **không có `[Authorize]`** |
| `AuthController` | Không đặt role ở class — chỉ thêm `[Authorize]` bare cho `Logout`, `ChangePassword`, `GetMe` | `Login/GoogleLogin/RefreshToken/ForgotPassword/ResetPassword` giữ public; `mock-admin-token` **không đụng tới** (ngoài phạm vi) |
| `ClassStudentsController` | `Admin,Academic,TrainingManager,Instructor` | |
| `ClassesController` | `Admin,Academic,TrainingManager` | |
| `CompletionRequirementsController` | `Admin,Academic,TrainingManager` | |
| `CoursesController` | `Admin,Academic,TrainingManager` | |
| `DashboardController` | `Admin,TrainingManager` | Trước đó **không có `[Authorize]`**; doc gốc ghi "Management" — map về role thật gần nhất (`TrainingManager`, vì hệ thống chỉ có 6 role cố định) |
| `DepartmentsController` | `Admin` | |
| `EnrollmentsController` | `Admin` | |
| `EtrController` | Class: `Admin,QA,Student,Instructor`; narrow theo action (xem mục 1.2) | |
| `EvidenceTypesController` | `Admin` | |
| `EvidencesController` | `Instructor,QA,Admin` (view/verify/delete), Upload giữ bare `[Authorize]` | |
| `ExportsController` | `Admin` | Trước đó **không có `[Authorize]`** |
| `PracticalChecklistResultsController` | `Admin,Instructor,QA` | |
| `PracticalChecklistsController` | `Admin,Instructor` | |
| `ReportsController` | `Admin,TrainingManager` | Trước đó **không có `[Authorize]`** |
| `RetakeHistoryController` | `Admin,QA,Instructor,TrainingManager,Academic` | Trước đó **không có `[Authorize]`** (mục 5 riêng, xem 1.3) |
| `SearchController` | bare `[Authorize]` (mọi role đã đăng nhập) | Trước đó **không có `[Authorize]`** |
| `SessionsController` | `Admin,Instructor` | |
| `SubjectSignoffController` | `Instructor,Academic` | |
| `SubjectsController` | `Admin,Academic,TrainingManager` | |
| `UserProfilesController` | `Admin` cho action quản trị, giữ bare cho `GetMyProfile`/`UpdateMyProfile` | |

**Lưu ý kỹ thuật quan trọng:** ASP.NET Core kết hợp `[Authorize]` ở class-level và method-level theo **AND**, không phải OR. Vì vậy với các controller có 1 action cố tình mở rộng hơn phần còn lại (`EtrController.GetMyEtrs`, `AttendanceController.GetAttendanceRecords`, `AssessmentResultsController.GetAssessmentResults`), **không thể** đặt Role hẹp ở class rồi nới ở method — làm vậy sẽ khoá luôn action đáng lẽ phải mở. Cách xử lý: để class ở mức tối thiểu cần thiết (hoặc bare `[Authorize]`), rồi gắn Role hẹp riêng cho từng action cần giới hạn.

### 1.2 [Mục 2] Khoá chặt `POST/PUT/DELETE /api/etr`

- **`POST /api/etr` (`CreateEtr`) — xoá hẳn.** Vi phạm trực tiếp nguyên tắc "ETR chỉ được hệ thống tự tạo khi ghi danh". Xoá luôn `IEtrService.CreateEtrAsync`/`EtrService.CreateEtrAsync` (không còn nơi nào khác gọi tới, đã `grep` xác nhận).
- **`PUT /api/etr/{id}` (`UpdateEtr`) — xoá hẳn.** `UpdateEtrRecordRequest` chỉ có 2 field `Status`/`IsLocked` — đúng 2 field không được phép ghi thô theo báo cáo. Không còn field hợp lệ nào khác để giữ lại endpoint này, nên xoá theo đúng phương án báo cáo đề xuất, thay vì cố giữ lại một endpoint rỗng.
- **`DELETE /api/etr/{id}` — giữ lại, nhưng giới hạn `Admin` only.** Đây là soft-delete, không đụng `Status`/`IsLocked` trực tiếp nên không phải là đường vòng pre-validation; đã có `ImmutabilityValidator` (tại `AppDbContext.SaveChangesAsync`) chặn mọi write/delete lên ETR đã `Completed`/`IsLocked` từ trước.
- Method-level role cho các action còn lại của `EtrController`: `SubmitEtr` → `Instructor,Admin`; `VerifyEtr`/`ReturnEtr` → `QA,Admin`; `CompleteEtr` → `Admin` (suy từ đúng comment `<response code="403">` sẵn có trong docstring của từng action).

### 1.3 [Mục 3] `ApprovalService.ProcessApprovalActionAsync` — nhánh Approve

Trước: khi `action == "Approve"`, code set thẳng `etr.Status = "Completed"; etr.IsLocked = true;` — bỏ qua toàn bộ gate của `CompleteEtrAsync` (mandatory subject Passed/Exempted, evidence Verified).

Sau: `ApprovalService` nhận thêm `IEtrService` qua constructor, nhánh Approve gọi `await _etrService.CompleteEtrAsync(request.ETRCourseRecordId, actionByAccountId, ct)` thay vì set field trực tiếp — vẫn nằm trong cùng transaction (`BeginTransactionAsync`/`CommitTransactionAsync`) nên nếu gate fail, toàn bộ approval action rollback theo, không có Approval "half-applied".

### 1.4 [Mục 5] `RetakeHistoryController`

Thêm `[Authorize(Roles = "Admin,QA,Instructor,TrainingManager,Academic")]` — trước đó hoàn toàn public, lộ lịch sử thi lại (điểm cũ/mới, lý do, người duyệt) của mọi học viên.

### 1.5 [Mục 7] Hash mật khẩu bằng bcrypt

- Thêm package `BCrypt.Net-Next` (4.0.3) vào `ETR.Application`, `ETR.Infrastructure`, `ETR.API`.
- `AccountService.CreateAccountAsync`: `PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)` thay vì lưu thẳng plaintext.
- `AuthController.Login`: verify bằng `BCrypt.Net.BCrypt.Verify(request.Password, account.PasswordHash)` thay vì so sánh chuỗi `==`.
- `DataSeeder.CreateAccount`: mật khẩu demo vẫn là `"123456"` (để dev/test đăng nhập được) nhưng giờ lưu dạng **hash**, không còn literal string trong code.

**Chưa xử lý (ngoài phạm vi P0 lần này):** migration cũ `20260719063637_SeedSystemData.cs` và các script raw SQL (`Deploy_ETR_System.sql`, `Deploy_NukeAndSeed.sql`, `ALL_IN_ONE_Deploy.sql`) vẫn insert `PasswordHash = '123456'` dạng plaintext. Sửa migration đã áp dụng là việc rủi ro/không hiệu quả (không ảnh hưởng DB đã tồn tại); cần xử lý riêng nếu muốn seed qua SQL thuần cũng ra hash.

### 1.6 [Mục 8] Loại `PasswordHash` khỏi payload AuditLog

`AppDbContext.Compliance.cs` → `SerializePropertyValues`: thêm `.Where(property => property.Name != nameof(Account.PasswordHash))` trước khi serialize `PropertyValues` thành JSON lưu vào `AuditLog.OldValue`/`NewValue`. Trước đây mọi thay đổi trên `Account` (kể cả tạo mới) đều serialize **toàn bộ property**, bao gồm cả hash mật khẩu, vào bảng audit — ai đọc được `AuditLogs` coi như đọc được hash để brute-force offline.

### 1.7 [Mục 9] `AuthController.GetMe`

Trước: `[HttpGet("me")] public async Task<ActionResult> GetMe([FromQuery] int accountId, ...)` — bất kỳ ai gọi `?accountId=X` bất kỳ đều lấy được thông tin tài khoản/profile của người khác (IDOR + enumeration toàn bộ user directory, không cần đăng nhập).

Sau: bỏ tham số `accountId` khỏi query, lấy từ `ICurrentUserService.AccountId` (JWT claim `NameIdentifier`), thêm `[Authorize]`. `AuthController` giờ nhận thêm `ICurrentUserService` qua constructor.

### 1.8 [Mục 10] `ImmutabilityValidator` — không còn chặn nhầm thao tác Unlock hợp lệ

Trước: `ValidateEtrRecord` chặn **mọi** write lên `ETRCourseRecord` nếu giá trị **gốc** (`OriginalEtrStatus`/`OriginalEtrIsLocked`) là Completed/Locked — kể cả chính thao tác `UnlockEtrAsync` (vốn có mục đích mở khoá bản ghi đang locked!). Kết quả: `UnlockEtrAsync` không bao giờ chạy được, gate "mở khoá đặc biệt" duy nhất của hệ thống bị hỏng hoàn toàn.

Sau: thêm field `IsBeingUnlocked` vào `EntityChangeSnapshot` (tính bằng cách so sánh **giá trị mới** với giá trị gốc: `OriginalValue is true && CurrentValue is false` cho `IsLocked`) — cùng pattern với `IsBeingUnpublished` đã có sẵn cho `AssessmentResult`. `ValidateEtrRecord` giờ bỏ qua guard nếu `change.IsBeingUnlocked == true`, chỉ với đúng transition Locked→Unlocked, không nới lỏng bất kỳ trường hợp nào khác.

---

## 1.9 Cập nhật sau `/mpower:flow-review-code` — 4 lỗi phát hiện & sửa khi review lại chính batch fix này

Sau khi áp dụng 8 mục P0 ở trên, đã chạy 2 agent review độc lập song song (`agent-techlead` review code correctness, `agent-security` review OWASP) trên diff thật (`git diff`, 914 dòng) — không phải re-audit toàn repo, chỉ verify đúng batch fix vừa làm. Cả 2 agent tự đọc code, tự đọc `.claude/context/domain.md`/`api.md` để đối chiếu role đúng với nghiệp vụ, không tin thẳng mô tả trong prompt.

**1. `[must-fix]` — Thiếu role `TrainingManager` ở `ApprovalsController` + `EtrController.CompleteEtr` (2 agent phát hiện độc lập, cùng 1 lỗi).** Batch fix lúc đầu gán `ApprovalsController` → `Admin,Instructor` và `CompleteEtr` → `Admin`, dựa theo XML doc/docstring sẵn có trên từng controller. Nhưng chính mục #19 của `BAO_CAO_REVIEW_HE_THONG.md` ("Chỉ TrainingManager Approve/Complete") và audit text có sẵn trong `EtrService.CompleteEtrAsync` ("...completed and locked by Training Manager") đều xác nhận TrainingManager mới là người duyệt thật — nếu không sửa, batch fix sẽ đóng gate cũ (ai cũng approve được) nhưng mở gate mới (không ai đúng role approve được, trừ Admin). **Đã sửa:** `ApprovalsController` → `Admin,Instructor,TrainingManager`; `EtrController.CompleteEtr` → `Admin,TrainingManager`. **Gap còn lại (chưa sửa, ngoài phạm vi):** `Instructor` vẫn có thể tự Approve chính ETR mình vừa submit (không tách biệt submit vs. approve ở mức attribute) — cần custom authorization theo `action` param nếu muốn siết hoàn toàn separation-of-duty.

**2. `[must-fix]` — `EvidencesController.UploadEvidence` thiếu role riêng.** Đây là 1 trong 19 controller mà mục 1 (P0) áp dụng role, nhưng khi rà lại action-by-action đã bỏ sót action này — chỉ kế thừa `[Authorize]` bare của class, khiến Student vẫn upload được evidence cho `SubjectResultId` bất kỳ (`UploadEvidenceRequest.AccountId`/`SubjectResultId` đều client-supplied, không có ownership check). **Đã sửa:** thêm `[Authorize(Roles="Instructor,Admin")]` riêng cho action này.

**3. `[should-fix]` — Timing oracle ở `AuthController.Login`.** Code cũ chỉ gọi `BCrypt.Verify` khi tìm thấy account (`account == null || ... || !BCrypt.Verify(...)` — short-circuit `||` bỏ qua `Verify` khi `account == null`), khiến "user không tồn tại" (~1ms) và "user tồn tại, sai mật khẩu" (~100ms do cost factor BCrypt) có thời gian phản hồi khác nhau rõ rệt — lộ username qua timing side-channel. **Đã sửa:** luôn gọi `BCrypt.Verify` kể cả khi không tìm thấy account, dùng một hash giả cố định (`DummyPasswordHash`, tính 1 lần lúc khởi tạo class) làm input trong trường hợp đó.

**4. `[should-fix]` — `IsBeingUnlocked` (mục 10) bypass toàn bộ entity thay vì đúng 1 field.** Cả 2 agent độc lập chỉ ra: điều kiện `IsBeingUnlocked` ban đầu chỉ dựa vào `IsLocked` đổi từ true→false, rồi bypass **toàn bộ** guard `ValidateEtrRecord` cho cả entry đó — nếu tương lai có code nào cùng lúc đổi `IsLocked` **và** `Status`/field khác trong cùng 1 lần `SaveChanges`, tất cả sẽ lọt qua mà guard không biết. Hiện tại `UnlockEtrAsync` chỉ đổi đúng `IsLocked` nên chưa bị khai thác thật, nhưng đây là lỗ hổng thiết kế (defense-in-depth). **Đã sửa:** `IsBeingUnlocked` giờ yêu cầu thêm **không có field nào khác** (`Status`, `SubmittedAt`, `VerifiedAt`, `CompletedAt`, `EnrollmentId`) bị đổi cùng lúc.

**Đã build lại (`dotnet build`) sau khi áp dụng cả 4 sửa trên — 0 error, cùng 4 warning cũ không liên quan.**

## 2. Đã kiểm chứng bằng cách nào

- `dotnet build` toàn solution: **0 Warning liên quan, 0 Error** (2 warning `CS8604` có sẵn từ trước ở `ExportsController.cs`, không liên quan tới các thay đổi này; 2 warning `NU1510` cũng có sẵn từ trước).
- Đã chạy full app (`dotnet run`) để verify DI wiring (`ApprovalService` nhận thêm `IEtrService`, `AuthController` nhận thêm `ICurrentUserService`) — **verify thành công, app khởi động sạch, seed đầy đủ, "Application started"**.
  - Lần chạy đầu tiên gặp lỗi tưởng là bug cũ đã biết (`InvalidOperationException: CourseSubject.CourseId is unknown...`, khớp mô tả trong `docs/maintain/2026-07-20_data-seeder-va-fix-schema-migration.md` mục 1.2.a) — nhưng khi bị hỏi lại, đã verify sâu hơn bằng cách connect thẳng vào SQL Server (Docker `sql_server`) thay vì chỉ dựa vào text lỗi khớp mô tả cũ:
    - Migration `20260720111815_FixSchemaDriftAndIdentitySeed` **đã** có trong `__EFMigrationsHistory` — migration fix đã áp dụng đúng.
    - Nhưng `Courses.CourseId = 0` ("AMT-101") và `Subjects.SubjectId = 0` ("SJ-REG") **đang tồn tại thật** trong DB lúc đó — đúng bug reseed-về-0, nhưng `CourseSubjects.CourseId`/`SubjectId` xác nhận **không phải cột identity** (`sys.columns.is_identity = 0`) — 0 rò vào từ 2 bảng cha (`Courses`/`Subjects`), không phải do `CourseSubjects`.
    - **Root cause thật:** `Deploy_NukeAndSeed.sql:39` vẫn còn `DBCC CHECKIDENT (..., RESEED, 0)` cho **mọi bảng**, đúng pattern lỗi cũ mà migration đã fix — migration fix chỉ là 1 hành động DDL lịch sử (chạy 1 lần khi áp dụng), không ngăn được script reset thủ công này tái tạo lại đúng bug mỗi lần ai đó dùng nó để reset DB dev.
    - Đọc kỹ `Deploy_NukeAndSeed.sql` phát hiện thêm: script này **đã lỗi thời nghiêm trọng hơn dự kiến** — seed theo model 5-role cũ (`Admin, CROStaff, Instructor, Mentor, Student`, không phải 6-role hiện tại), insert vào `ETRCourseRecords` bằng cột `TotalScore/FinalGrade/Remarks` (không còn tồn tại trên entity hiện tại), và tự thêm 1 dòng migration giả `20260719015135_CleanUpAndSeedV2` vào `__EFMigrationsHistory` (migration này không có file thật trong `ETR.Infrastructure/Migrations/`). **Không sửa/không chạy script này** (ngoài phạm vi, cần viết lại toàn bộ nếu muốn dùng tiếp).
    - Đã **tự sửa DB dev** theo cách an toàn hơn: `DELETE` toàn bộ data ở 28 bảng (tắt/bật lại FK constraint tạm thời, **không** đụng `__EFMigrationsHistory`, **không** chạy `DBCC CHECKIDENT RESEED` — SQL Server không tự reset identity counter khi `DELETE`, nên không cần reseed và cũng tránh lặp lại chính bug đang muốn tránh), sau đó chạy lại `dotnet run` để `DataSeeder.cs` (nguồn seed chuẩn hiện tại) seed lại từ đầu — verify: `Courses.CourseId=1`, `Subjects` id 4-7 (không phải 1-4 vì identity counter tiếp tục từ giá trị cũ, nhưng đều khác 0, không sao), `CourseSubjects`=4 dòng, `ETRCourseRecords` cuối cùng đúng `Status=Completed, IsLocked=1`. App start sạch, không còn lỗi.
- Không có test project trong repo (`.claude/context/stack.md`) nên không chạy được unit/integration test tự động — verify chủ yếu qua đọc code + build + kiểm tra DI graph + 1 lần chạy full app thật với DB Docker.

## 3. Rủi ro/việc còn lại (ngoài phạm vi lần fix này)

- Mục 4 (`GET /api/auth/mock-admin-token` cấp JWT Admin ẩn danh) và mục 6 (xoay JWT signing key + mật khẩu DB `sa`, gỡ khỏi git history) — **chưa làm**, chủ động loại khỏi phạm vi theo yêu cầu.
- **`Deploy_NukeAndSeed.sql` đã lỗi thời, không nên dùng cho tới khi viết lại:** vẫn seed model 5-role cũ, insert sai cột `ETRCourseRecords` (`TotalScore/FinalGrade/Remarks` không còn tồn tại), tự thêm migration giả vào `__EFMigrationsHistory`, và vẫn còn đúng bug reseed-về-0 mà migration `FixSchemaDriftAndIdentitySeed` đã fix cho đường migration (nhưng không fix cho script thủ công này). Muốn reset DB dev sạch: xoá data thủ công (giữ `__EFMigrationsHistory`, không reseed identity) rồi để `dotnet run` gọi `DataSeeder.cs` seed lại — **không dùng** `Deploy_NukeAndSeed.sql` cho tới khi có người viết lại khớp schema/6-role hiện tại.
- 2 DTO `CreateEtrRecordRequest`/`UpdateEtrRecordRequest` giờ không còn được dùng ở backend (chỉ còn tham chiếu trong `Frontend_Assets/api-collection.ts`, file đó không nằm trong build backend) — **chưa xoá file**, để lại chờ xác nhận riêng (theo policy: xoá file cần hỏi trước).
- Migration `SeedSystemData` cũ + các script `.sql` deploy vẫn còn seed mật khẩu plaintext `"123456"` — xem ghi chú ở mục 1.5.
- Các finding S1/S2 khác trong `BAO_CAO_REVIEW_HE_THONG.md` (IDOR attendance/assessment, Reject/Return 2 luồng không đồng bộ, AttendanceRate sai mẫu số, v.v.) **không nằm trong phạm vi P0 lần này**, vẫn `still_open`.

## 4. Files liên quan

| File | Trạng thái |
|---|---|
| 27 file trong `ETR.API/Controllers/*.cs` | Sửa — thêm/chỉnh `[Authorize(Roles=...)]`; `EtrController.cs` xoá thêm 2 action |
| `ETR.API/Controllers/AuthController.cs` | Sửa thêm — `GetMe`, `Login`, DI |
| `ETR.API/ETR.API.csproj` | Thêm `PackageReference BCrypt.Net-Next` |
| `ETR.Application/Compliance/ImmutabilityValidator.cs` | Sửa — thêm `IsBeingUnlocked` |
| `ETR.Application/ETR.Application.csproj` | Thêm `PackageReference BCrypt.Net-Next` |
| `ETR.Application/Interfaces/IEtrService.cs` | Xoá `CreateEtrAsync`/`UpdateEtrAsync` |
| `ETR.Application/Services/AccountService.cs` | Sửa — hash mật khẩu |
| `ETR.Application/Services/ApprovalService.cs` | Sửa — Approve gọi `CompleteEtrAsync` |
| `ETR.Application/Services/EtrService.cs` | Xoá `CreateEtrAsync`/`UpdateEtrAsync` |
| `ETR.Infrastructure/Data/AppDbContext.Compliance.cs` | Sửa — loại `PasswordHash` khỏi audit payload, thêm `IsBeingUnlocked` |
| `ETR.Infrastructure/Data/DataSeeder.cs` | Sửa — hash mật khẩu demo |
| `ETR.Infrastructure/ETR.Infrastructure.csproj` | Thêm `PackageReference BCrypt.Net-Next` |
