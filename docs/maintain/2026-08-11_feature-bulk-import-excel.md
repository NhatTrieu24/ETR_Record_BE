Ch# Feature: Bulk Import via Excel — 2026-08-11

**Ngày thực hiện:** 2026-08-11  
**Phạm vi:** `ETR.Application/DTOs/Import/` (5 file mới); `ETR.Application/Interfaces/IImportService.cs` (mới); `ETR.Application/Services/ImportService.cs` (mới); `ETR.Application/DependencyInjection.cs` (đăng ký DI); `ETR.API/Controllers/ImportController.cs` (mới — 6 endpoints).  
**Mục tiêu:** Giải quyết các thao tác manual tốn thời gian được ghi nhận trong `ETR.Documentation/final/addition-4.md`: Instructor phải nhập điểm danh, điểm lý thuyết, điểm thực hành từng học viên một. Tính năng này cho phép import hàng loạt (bulk) qua file Excel, kèm bước validate dry-run trước khi commit.

---

## 1. Vấn đề trước khi làm

Instructor có lớp 30 học viên × 5 buổi học = **150 lần gọi API điểm danh**. Nhân thêm điểm lý thuyết + thực hành, tổng có thể vượt 300-500 thao tác đơn lẻ mỗi khóa. Hoàn toàn không có luồng bulk nào.

---

## 2. Thiết kế

### 2.1 Luồng 2-bước (Validate → Commit)

```
FE tải template về       →  GET /api/import/{type}/template?{id}
Instructor điền file     →  (offline)
FE upload để kiểm tra    →  POST /api/import/{type}/validate   (dry-run, không ghi DB)
BE trả ImportValidationResult (CanCommit, Errors[])
Nếu sạch, FE confirm     →  POST /api/import/{type}/commit     (ghi DB, atomic transaction)
```

### 2.2 Template server-generated

Template **không phải file tĩnh** — được sinh động từ DB theo session/assessment cụ thể:
- Header row 1: Tiêu đề có SessionTitle / ComponentName
- Row 2: Metadata (Id các entity để validate lại phía server)
- Row 3: Tên cột (bold, màu xanh nhạt)
- Row 4+: Danh sách học viên đã pre-fill (Instructor chỉ điền cột giá trị)

Cột read-only (EnrollmentId, FullName, UserCode, AccountId, SubjectResultId) có nền xám để phân biệt.  
Cột Status có **dropdown validation** (Present / Absent / Late) trong Excel.

### 2.3 Ownership — tái dùng ClassOwnershipValidator

Instructor chỉ được commit cho môn mình được phân công (`ClassSubject.InstructorAccountId`). Check dùng lại `ClassOwnershipValidator.EnsureInstructorOwnsSubject` — nhất quán với logic Attendance/AssessmentResult đơn lẻ.

### 2.4 Idempotency / duplicate safety

- **Attendance**: Nếu `(SessionId, EnrollmentId)` đã tồn tại trong DB → báo lỗi trong `Errors[]`, không rollback phần còn lại của validate nhưng **commit bị block** (all-or-nothing).
- **Assessment**: Nếu có placeholder `Pending` → ghi đè (đúng flow chuẩn của `RecordAssessmentScoreAsync`). Nếu đã tồn tại điểm không phải Pending → đưa vào `Errors[]`, skip.

---

## 3. API mới

### Attendance

| Method | Endpoint | Query | Auth |
|---|---|---|---|
| GET | `/api/import/attendance/template` | `?sessionId=` | Instructor, Academic, Admin |
| POST multipart | `/api/import/attendance/validate` | `?sessionId=` | Instructor, Academic, Admin |
| POST multipart | `/api/import/attendance/commit` | `?sessionId=` | Instructor, Academic, Admin |

### Assessment (lý thuyết & thực hành dùng chung — phân biệt qua `Assessment.AssessmentType`)

| Method | Endpoint | Query | Auth |
|---|---|---|---|
| GET | `/api/import/assessment/template` | `?assessmentId=` | Instructor, Academic, Admin |
| POST multipart | `/api/import/assessment/validate` | `?assessmentId=` | Instructor, Academic, Admin |
| POST multipart | `/api/import/assessment/commit` | `?assessmentId=` | Instructor, Academic, Admin |

### Response schemas

**ImportValidationResult** (validate endpoint):
```json
{
  "totalRows": 30,
  "validRows": 28,
  "errorRows": 2,
  "canCommit": false,
  "errors": [
    { "row": 5,  "column": "Status",       "message": "Giá trị 'Pesent' không hợp lệ. Chấp nhận: Present, Absent, Late." },
    { "row": 12, "column": "EnrollmentId", "message": "EnrollmentId 999 không thuộc lớp của session này." }
  ]
}
```

**ImportCommitResult** (commit endpoint):
```json
{
  "imported": 30,
  "skipped": 0,
  "errors": []
}
```

---

## 4. Cấu trúc file mới

| File | Loại |
|---|---|
| `ETR.Application/DTOs/Import/ImportRowError.cs` | DTO |
| `ETR.Application/DTOs/Import/ImportValidationResult.cs` | DTO |
| `ETR.Application/DTOs/Import/ImportCommitResult.cs` | DTO |
| `ETR.Application/DTOs/Import/AttendanceImportRow.cs` | DTO (internal parse model) |
| `ETR.Application/DTOs/Import/AssessmentImportRow.cs` | DTO (internal parse model) |
| `ETR.Application/Interfaces/IImportService.cs` | Interface |
| `ETR.Application/Services/ImportService.cs` | Service |
| `ETR.API/Controllers/ImportController.cs` | Controller |

---

## 5. Template mẫu — Cấu trúc chi tiết

### 5.1 Attendance Template (`attendance_session_{id}.xlsx`)

| Cột | Header | Pre-filled | Do user điền |
|---|---|---|---|
| A | `EnrollmentId` | ✅ (nền xám) | ❌ |
| B | `Họ và tên` | ✅ (nền xám) | ❌ |
| C | `Mã học viên` | ✅ (nền xám) | ❌ |
| D | `Trạng thái (Present/Absent/Late)*` | ❌ | ✅ (dropdown) |
| E | `Ghi chú` | ❌ | ✅ (tự do) |

**Row 1:** `BẢNG ĐIỂM DANH - {SessionTitle} - {ClassCode}`  
**Row 2:** `SessionId: X | ClassId: Y | SubjectId: Z | Ngày: DD/MM/YYYY | Môn: {SubjectName}`  
**Row 3:** Headers (bold, nền xanh nhạt)  
**Row 4+:** Dữ liệu học viên

### 5.2 Assessment Template (`assessment_{id}.xlsx`)

| Cột | Header | Pre-filled | Do user điền |
|---|---|---|---|
| A | `AccountId` | ✅ (nền xám) | ❌ |
| B | `Họ và tên` | ✅ (nền xám) | ❌ |
| C | `Mã học viên` | ✅ (nền xám) | ❌ |
| D | `SubjectResultId` | ✅ (nền xám) | ❌ |
| E | `Điểm (0-100)* [Đạt: ≥X]` | ❌ | ✅ (số thập phân) |
| F | `Ghi chú` | ❌ | ✅ (tự do) |

**Row 1:** `BẢNG NHẬP ĐIỂM - {ComponentName} ({AssessmentType}) - Môn: {SubjectName}`  
**Row 2:** `AssessmentId: X | CourseId: Y | PassingScore: Z | Weight: W | Type: Theory/Practical`  
**Row 3:** Headers  
**Row 4+:** Dữ liệu học viên

---

## 6. Dữ liệu mẫu — Seed DB hiện tại

> Lấy từ DataSeeder. Dùng các ID này để test trực tiếp trên Swagger.

### Session IDs (auto-provisioned khi tạo Class)

Xem bảng `Sessions` sau khi tạo Class — mỗi ClassSubject.RequiredSessions buổi được tạo tự động.  
Ví dụ query: `SELECT TOP 5 SessionId, ClassId, SubjectId, SessionTitle, IsConfirmed FROM Sessions WHERE IsDeleted = 0`

### Assessment IDs

Xem bảng `Assessments` theo CourseId:  
`SELECT AssessmentId, ComponentName, AssessmentType, PassingScore, Weight FROM Assessments WHERE IsDeleted = 0`

### Quy trình test end-to-end

```
1. GET /api/import/attendance/template?sessionId=1
   → Tải file, mở Excel, điền cột D (Status) cho từng học viên

2. POST /api/import/attendance/validate?sessionId=1
   Content-Type: multipart/form-data, file=<file vừa điền>
   → Kiểm tra response: canCommit=true, errors=[]

3. POST /api/import/attendance/commit?sessionId=1
   Content-Type: multipart/form-data, file=<file vừa điền>
   → Response: { imported: N, skipped: 0, errors: [] }

4. Tương tự cho assessment: thay sessionId bằng assessmentId
```

---

## 7. Giới hạn đã biết / Chưa làm

- **Không có giới hạn max rows** — chấp nhận ở quy mô hiện tại (max vài trăm học viên/lớp). Nếu scale lên, cần thêm `if (rows.Count > 500) throw`.
- **Evidence import** chưa triển khai — addition-4.md có đề cập nhưng Evidence gồm binary file + metadata, cần thiết kế riêng (out of scope lần này).
- **Commit không idempotent hoàn toàn cho Assessment** — nếu điểm đã tồn tại dạng non-Pending, row đó bị skip với thông báo trong `Errors[]`. Dùng `PUT /api/assessmentresults/{id}` để sửa.
- **Validate và Commit upload file 2 lần** — nếu FE muốn tối ưu, có thể bỏ validate riêng và gọi thẳng commit (commit cũng tự validate trước khi ghi).

---

## 8. Files liên quan

| File | Trạng thái |
|---|---|
| `ETR.Application/DTOs/Import/ImportRowError.cs` | Mới |
| `ETR.Application/DTOs/Import/ImportValidationResult.cs` | Mới |
| `ETR.Application/DTOs/Import/ImportCommitResult.cs` | Mới |
| `ETR.Application/DTOs/Import/AttendanceImportRow.cs` | Mới |
| `ETR.Application/DTOs/Import/AssessmentImportRow.cs` | Mới |
| `ETR.Application/Interfaces/IImportService.cs` | Mới |
| `ETR.Application/Services/ImportService.cs` | Mới |
| `ETR.API/Controllers/ImportController.cs` | Mới |
| `ETR.Application/DependencyInjection.cs` | Sửa — thêm `IImportService` |
| `docs/sample-data/attendance_import_sample.csv` | Mới — file mẫu tham khảo |
| `docs/sample-data/assessment_import_sample.csv` | Mới — file mẫu tham khảo |
