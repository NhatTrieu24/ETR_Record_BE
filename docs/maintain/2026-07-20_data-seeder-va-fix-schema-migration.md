# DataSeeder.cs (C#) + Fix schema drift/identity seed — 2026-07-20

**Ngày thực hiện:** 2026-07-20
**Phạm vi:** `ETR.Infrastructure/Data/DataSeeder.cs`, 1 migration mới, `AppDbContext.cs`
**Mục tiêu:** Chuyển toàn bộ seed data từ raw SQL trong migration (`SeedSystemData`) sang C# tập trung trong `DataSeeder.cs`, chia module rõ ràng, để sau này chỉ cần sửa 1 file rồi chạy lại app là có data mới.

---

## 1. Tóm tắt những gì đã thay đổi

### 1.1 `ETR.Infrastructure/Data/DataSeeder.cs` (viết lại toàn bộ)

Trước đây file này là no-op (`// NO DATA SEEDING IN C#`). Giờ chứa **11 module seed**, chạy tuần tự trong `SeedAsync`, mỗi module tự kiểm tra bảng rỗng (`if (!await context.X.AnyAsync())`) trước khi insert — **idempotent**, chạy lại bao nhiêu lần cũng không trùng data:

| # | Module | Bảng | Ghi chú |
|---|---|---|---|
| 1 | Identity | `Roles`, `Departments`, `Accounts`, `UserProfiles` | 6 role cố định, 2 department, 6 account mẫu (admin/instructor/qa/academic/manager/student) |
| 2 | Catalog / Curriculum | `Courses`, `Subjects`, `CourseSubjects`, `CompletionRequirements`, `Assessments`, `PracticalChecklists`, `EvidenceTypes` | 1 course "AMT-101", 4 subject, đủ assessment + checklist item |
| 3 | Class & Scheduling | `Classes`, `Sessions` | 1 lớp, 4 buổi học (1 buổi/subject) |
| 4 | Enrollment | `CourseEnrollments`, `ClassStudents` | 1 học viên ghi danh vào lớp |
| 5 | ETR & Subject Results | `ETRCourseRecords`, `SubjectResults` | Tạo ở trạng thái `InProgress`, chưa khoá |
| 6 | Attendance | `AttendanceRecords` | Điểm danh "Present" cho cả 4 buổi |
| 7 | Assessment Results & Retake | `AssessmentResults`, `RetakeHistories` | Có 1 case học lại (attempt 1 rớt, attempt 2 đậu) để demo luồng retake |
| 8 | Practical Checklist Results | `PracticalChecklistResults` | Hoàn thành cả 4 hạng mục checklist |
| 9 | Signoff | `SubjectSignoffs` | Instructor ký xác nhận từng subject |
| 10 | Evidence | `EvidenceFiles` | 2 file minh chứng, đã QA verify |
| 11 | Approval Workflow | `ApprovalRequests`, `ApprovalHistories` | Đi hết luồng Submit → Review → Approve; cuối cùng **update** `ETRCourseRecord` sang `Completed` + `IsLocked = true` (mô phỏng đúng nghiệp vụ thật, không set khoá ngay từ đầu) |

**Không seed:** `AuditLogs` (tự động sinh bởi `AppDbContext.Compliance.cs` mỗi khi có Insert/Update, không được ghi tay), `DashboardSnapshot`/`ExportJob` (dữ liệu rollup/job runtime, không phải seed data tĩnh).

**Mật khẩu:** vẫn để plaintext `"123456"` giống seed cũ (`SeedSystemData` migration) — codebase hiện chưa có service hash mật khẩu nào để dùng, nên chưa xử lý ở đây. **Cần làm riêng khi có Auth service thật.**

### 1.2 Migration mới: `20260720111815_FixSchemaDriftAndIdentitySeed`

Trong lúc verify DataSeeder chạy thật trên SQL Server (Docker), phát hiện **2 lỗi có sẵn từ trước** (không do DataSeeder gây ra) chặn đứng bất kỳ insert nào vào 2 bảng bên dưới — migration mới fix cả hai:

**a) Reseed identity về sai giá trị (0) cho mọi bảng đang rỗng**
`SeedSystemData` (migration cũ) chạy `EXEC sp_MSForEachTable 'DBCC CHECKIDENT (?, RESEED, 0)'` trên **toàn bộ bảng**, không chỉ 3 bảng nó thực sự seed (Roles/Departments/Accounts). SQL Server có quy tắc đặc biệt: reseed về 0 trên bảng đang rỗng khiến **dòng đầu tiên insert vào nhận ID = 0** — trùng với giá trị mặc định của `int` trong C#. Với bảng có khoá chính ghép từ FK (như `CourseSubject`), EF Core không phân biệt được "ID thật = 0" với "chưa gán giá trị", nên báo lỗi:
```
InvalidOperationException: The value of 'CourseSubject.CourseId' is unknown when attempting to
save changes. This is because the property is also part of a foreign key for which the
principal entity in the relationship is not known.
```
→ Migration mới reseed lại **mọi bảng identity đang rỗng** về mốc an toàn (dòng tiếp theo bắt đầu từ 1).

**b) Thiếu cột do migration bị bỏ sót**
- `AssessmentResults` thiếu cột `SessionId` — code (`AppDbContext.cs`) đã cấu hình FK/index cho cột này nhưng chưa migration nào tạo cột thật (chỉ `PracticalChecklistResults` được thêm `SessionId` đúng cách ở migration `AddSessionIdToPracticalChecklistResult`).
- `PracticalChecklistResults` thiếu 4 cột `Score`, `ResultStatus`, `IsPublished`, `PublishedAt` (entity đã đổi từ `IsCompleted` sang các cột này nhưng chưa có migration tương ứng).

Đã xác nhận đây là **toàn bộ** phần chênh lệch còn lại bằng cách diff schema thật (một DB build từ migration history, một DB build fresh từ model hiện tại) — không còn drift nào khác ngoài 2 mục trên.

### 1.3 `ETR.Infrastructure/Data/AppDbContext.cs`

Không có thay đổi thực chất (đã thử thêm `ValueGeneratedNever()` cho `CourseSubject.CourseId/SubjectId` để chẩn đoán lỗi (a) ở trên, sau đó xác nhận không cần thiết — nguyên nhân gốc là (a), không phải cấu hình composite key — nên đã revert lại y nguyên).

---

## 2. Hướng dẫn — Môi trường Dev (local)

### 2.1 Lần đầu setup (DB chưa có gì)

```bash
dotnet ef database update --project ETR.Infrastructure --startup-project ETR.API
dotnet run --project ETR.API
```
`Program.cs` đã tự gọi `Database.MigrateAsync()` rồi `DataSeeder.SeedAsync()` mỗi lần khởi động — không cần thao tác gì thêm. Toàn bộ 11 module sẽ chạy vì mọi bảng đang rỗng.

### 2.2 Muốn đổi seed data (thêm/sửa course, subject, account mẫu...)

1. Sửa trực tiếp trong `ETR.Infrastructure/Data/DataSeeder.cs` (mỗi module đã tách rõ theo comment `// ===== Module: ... =====`).
2. **Xoá data cũ nhưng giữ nguyên migration history** bằng script có sẵn:
   ```bash
   sqlcmd -S localhost -U sa -P <password> -i Deploy_NukeAndSeed.sql
   ```
   (script này xoá toàn bộ data + reseed identity, **không** xoá bảng `__EFMigrationsHistory` — nên khi chạy lại app, EF sẽ **không** áp dụng lại các migration cũ, chỉ có `DataSeeder.SeedAsync()` chạy và insert data mới của bạn).
3. Chạy lại `dotnet run --project ETR.API` — data mới sẽ được seed vì mọi bảng đang rỗng lúc này.

> ⚠️ Nếu **không** nuke DB trước, các bảng đã có data (`Roles`/`Departments`/`Accounts`/`Courses`/... từ lần seed trước) sẽ bị bỏ qua do guard `AnyAsync()` — sửa file mà không nuke DB thì sẽ **không** thấy thay đổi.

### 2.3 Muốn seed thêm module mới hoàn toàn (bảng mới chưa từng có data)

Không cần nuke gì cả — cứ thêm method `SeedXxxAsync` mới, gọi trong `SeedAsync()`, chạy lại app là insert luôn (vì bảng đó đang rỗng).

---

## 3. Hướng dẫn — Môi trường Deploy (staging / production)

### 3.1 Áp dụng migration mới

Migration `FixSchemaDriftAndIdentitySeed` **bắt buộc phải chạy** trước khi bất kỳ code nào ghi vào `AssessmentResults` hoặc `PracticalChecklistResults` — nếu không sẽ lỗi `Invalid column name`. Chạy theo 1 trong 2 cách:

```bash
# Cách 1: CLI trực tiếp
dotnet ef database update --project ETR.Infrastructure --startup-project ETR.API

# Cách 2: tự động (mặc định) — Program.cs gọi MigrateAsync() khi app khởi động,
# chỉ cần deploy code mới và restart service là migration tự áp dụng.
```

### 3.2 Rủi ro cần lưu ý trên DB **đã có data thật** (không phải DB test/dev rỗng)

- **Cột thêm vào `AssessmentResults`/`PracticalChecklistResults`**: an toàn, chỉ `ADD COLUMN` với default value cho các cột `NOT NULL` (`Score` default `0`, `ResultStatus` default rỗng, `IsPublished` default `false`) — không mất data cũ, không cần downtime dài.
- **`DBCC CHECKIDENT ... RESEED 1`**: migration này **chỉ áp dụng cho bảng đang RỖNG** (`AND NOT EXISTS (SELECT 1 FROM ?)`) — bảng nào đã có data thật sẽ **không** bị đụng tới, an toàn tuyệt đối cho production đã vận hành.
- **Nếu production đã từng insert dữ liệu thật vào bảng nào đó với ID = 0** (trường hợp cực hiếm, chỉ xảy ra nếu đã từng có code khác từng ghi vào bảng đó trước khi phát hiện bug này) — cần kiểm tra thủ công trước khi deploy, vì migration này không tự sửa dữ liệu đã tồn tại, chỉ ngăn bug tái diễn cho bảng còn trống.

### 3.3 DataSeeder.cs có chạy trên production không?

Có — `Program.cs` gọi `DataSeeder.SeedAsync()` ở **mọi môi trường**, không phân biệt Development/Production. Vì mỗi module tự guard bằng `AnyAsync()`, trên production (đã có data thật) **toàn bộ 11 module sẽ tự bỏ qua** — không có rủi ro tạo nhầm data demo (course "AMT-101", account mẫu...) đè lên data thật, miễn là các bảng liên quan **đã có ít nhất 1 dòng** trước khi migration mới chạy.

> Nếu muốn tắt hẳn seeder trên production để chắc chắn 100%, có thể bọc lời gọi `DataSeeder.SeedAsync(context)` trong `Program.cs` bằng điều kiện `if (app.Environment.IsDevelopment())` — hiện tại **chưa làm** vì nằm ngoài phạm vi yêu cầu ban đầu, cần xác nhận thêm nếu muốn áp dụng.

---

## 4. Đã kiểm chứng bằng cách nào

Không chỉ `dotnet build` — đã dựng SQL Server thật (Docker `mcr.microsoft.com/mssql/server:2022-latest`), chạy full app (`Database.MigrateAsync()` + `DataSeeder.SeedAsync()`) trên DB hoàn toàn rỗng, verify:
- Toàn bộ 26 bảng liên quan có đúng số dòng theo kế hoạch (vd. `Roles=6`, `CourseSubjects=4`, `AssessmentResults=6`, `ApprovalHistories=3`...).
- `ETRCourseRecord` cuối cùng đúng trạng thái `Completed`, `IsLocked=1`.
- Chạy lại app lần 2 trên cùng DB — không có lỗi, số dòng **không đổi** (xác nhận idempotent).
- Diff schema thật giữa DB build từ migration history và DB build fresh từ model hiện tại — xác nhận không còn drift nào khác ngoài 2 mục đã fix ở mục 1.2.

---

## 5. Files liên quan

| File | Trạng thái |
|---|---|
| `ETR.Infrastructure/Data/DataSeeder.cs` | Viết lại toàn bộ |
| `ETR.Infrastructure/Migrations/20260720111815_FixSchemaDriftAndIdentitySeed.cs` | Mới |
| `ETR.Infrastructure/Migrations/20260720111815_FixSchemaDriftAndIdentitySeed.Designer.cs` | Mới |
| `ETR.Infrastructure/Migrations/AppDbContextModelSnapshot.cs` | Cập nhật tự động theo migration mới |
