# Feature: Export "Training Package" phục vụ kiểm toán CAA — tách PDF, evidence file thật, thêm Excel — 2026-07-24

**Ngày thực hiện:** 2026-07-24
**Phạm vi:** `ETR.Application/Services/ExportService.cs`, `ETR.Application/ETR.Application.csproj` (thêm `ClosedXML`), mới: `ETR.Application.Tests/` (xUnit project + `ExportServiceTests.cs`), `ETRSystem.slnx`.
**Mục tiêu:** Triển khai chức năng Export "Training Package" theo đúng luồng nghiệp vụ trong phân hệ Reporting, Search & Audit Management — trích xuất hồ sơ đào tạo của 1 học viên phục vụ thanh tra/kiểm toán CAA (Civil Aviation Authority): Tìm Kiếm → chọn ETR → Export → Training Package → hệ thống đóng gói `[Mã_học_viên]_[Mã_lớp]_Training_Package.zip`. Mở rộng thêm định dạng Excel theo yêu cầu bổ sung.

---

## 1. Tóm tắt những gì đã implement

### 1.1 Tách 1 PDF gộp thành 2 PDF riêng biệt

Trước: `ExportTrainingPackageAsync` dựng **1 PDF duy nhất** gồm ETR Summary + Subject Results + bảng liệt kê tên Evidence Files + bảng Approval Audit Trail, gộp chung không đúng với yêu cầu 3 thành phần tách bạch.
Đã tách `BuildPdf` thành 2 hàm PDF riêng:
- `BuildEtrSummaryPdf(...)` → `ETR_Summary.pdf`: thông tin học viên/lớp/khoá, trạng thái, mốc thời gian (Submitted/Verified/Completed), bảng Subject Results (mã môn, tên môn, trạng thái, điểm, attendance %).
- `BuildAuditHistoryPdf(...)` → `Audit_History.pdf`: bảng Approval Audit Trail (Action, Transition, At, Comment) — tách riêng hoàn toàn khỏi ETR Summary theo đúng yêu cầu.

### 1.2 Copy file evidence THẬT vào zip (thay vì chỉ liệt kê tên)

Trước: chỉ có 1 bảng liệt kê `FileName`/`VerificationStatus`/`VerifiedAt` trong PDF — không có file thật nào được đính kèm.
Đã sửa: với mỗi `EvidenceFile` thuộc các `SubjectResult` của ETR, đọc file thật từ `wwwroot/{FilePath}` và copy nguyên vẹn vào zip dưới thư mục `Evidence/{EvidenceFileId}_{FileName}` (prefix ID để tránh trùng tên giữa nhiều evidence có cùng tên file gốc). Đã verify byte-for-byte giống hệt bản gốc bằng `diff`.
Nếu file vật lý bị thiếu trên đĩa (DB có record nhưng file không tồn tại — gặp thật khi test với data seed dùng path giả `/evidence/amt101/...`), hệ thống **bỏ qua file đó và ghi log Warning** thay vì crash toàn bộ export — vẫn trả về gói với các phần còn lại.

### 1.3 Đổi tên file zip đúng format yêu cầu

Trước: `training-package_etr{id}_{timestamp}.zip`.
Đã sửa: `ExportService.BuildTrainingPackageZipFileName(studentCode, classCode)` → `"{studentCode}_{classCode}_Training_Package.zip"` (VD: `STU-01_AMT101-C1_Training_Package.zip`), lấy `studentCode` từ `UserProfile.UserCode` và `classCode` từ `Class.ClassCode`.
Sanitize ký tự không hợp lệ bằng bảng ký tự cấm cứng (`< > : " / \ | ?  *` + control chars) thay vì `Path.GetInvalidFileNameChars()` — API này phụ thuộc HĐH của **server** (macOS/Linux chỉ cấm `/` và NUL, không cấm `:`/`*`/`?`), trong khi file này được auditor tải về và mở trên máy **Windows** (rất có thể) — phải an toàn theo quy tắc Windows bất kể server chạy trên HĐH nào.
Guard thêm: nếu ETR không có `UserProfile` (enrollment không có hồ sơ học viên), throw `BusinessRuleViolationException` rõ ràng thay vì tạo ra tên file rác.

### 1.4 Thêm định dạng Excel (`ETR_Summary.xlsx`) — yêu cầu bổ sung

Theo yêu cầu mở rộng sau khi đã build xong bản PDF, đã thêm gói `ClosedXML` (MIT license) và hàm `ExportService.BuildEtrSummaryExcel(...)` — cùng nội dung với `ETR_Summary.pdf` (thông tin ETR, Subject Results) nhưng ở dạng bảng tính thật (`.xlsx`), để auditor lọc/sắp xếp dữ liệu được thay vì chỉ đọc PDF tĩnh.
Khác với PDF: `Class Code`/`Class Name` và `Course Code`/`Course Name` được tách thành 2 ô riêng (PDF gộp chung `"Code — Name"` để hiển thị, nhưng Excel cần dữ liệu nguyên tử mỗi ô mới lọc/sort được).
Zip nay có **4 thành phần**: `ETR_Summary.pdf`, `Audit_History.pdf`, `ETR_Summary.xlsx`, `Evidence/*`.

### 1.5 Thiết lập test project (TDD) — chưa từng có trong repo

Repo trước đây **không có project test thật nào** (`TestApp/` chỉ là console app scaffold rỗng, không nằm trong `.slnx`). Đã tạo `ETR.Application.Tests` (xUnit), thêm vào solution, áp dụng TDD cho phần logic thuần (không phụ thuộc DB/file thật):
- `BuildTrainingPackageZipFileName`: test format đúng, test sanitize ký tự nguy hiểm trên Windows, test throw `ArgumentException` khi thiếu mã học viên/lớp.
- `BuildEtrSummaryExcel`: test round-trip qua `ClosedXML` (dựng workbook trong memory → đọc lại → assert đúng cell chứa student code, class code, subject code, status), test case `studentProfile == null` trả về placeholder `"(unknown)"` thay vì crash.
- Toàn bộ phần orchestration DB-heavy/file-IO-heavy (`ExportTrainingPackageAsync`) verify bằng smoke test API thật (không mock DB), theo đúng pattern đã dùng xuyên suốt dự án.

---

## 2. Cách sử dụng API Export

### 2.1 Kích hoạt export

```
POST /api/exports/training-package
Authorization: Bearer <token JWT — role Admin>
Content-Type: application/json

{
  "userId": 0,
  "etrCourseRecordId": 1
}
```

- **`userId`**: field có sẵn trong DTO `ExportRequest` nhưng **không được dùng** ở endpoint này — "ai thực hiện export" lấy từ token JWT (`ICurrentUserService.AccountId`), không phải từ body. Điền số gì cũng được, kể cả `0`.
- **`etrCourseRecordId`**: bắt buộc, phải trỏ đến 1 `ETRCourseRecord` đang ở trạng thái `"Completed"` — nếu không sẽ nhận `400 Business rule violation`.

Response (`200`):
```json
{
  "exportJobId": 5,
  "requestedByAccountId": 7,
  "exportType": "TrainingPackage",
  "fileName": "STU-01_AMT101-C1_Training_Package.zip",
  "filePath": "uploads/exports/STU-01_AMT101-C1_Training_Package.zip",
  "status": "Completed",
  "requestedAt": "2026-07-24T09:34:42Z",
  "completedAt": "2026-07-24T09:34:42Z",
  "downloadExpiredAt": "2026-07-31T09:34:42Z",
  "etrCourseRecordId": 1
}
```

### 2.2 Tải file zip

```
GET /api/exports/download/{exportJobId}
Authorization: Bearer <token>
```

Dùng `exportJobId` nhận được ở bước 2.1. Trả về file zip thật (`application/octet-stream`), giải nén sẽ thấy:
```
STU-01_AMT101-C1_Training_Package.zip
├── ETR_Summary.pdf       (thông tin ETR + bảng Subject Results)
├── Audit_History.pdf     (lịch sử phê duyệt/audit trail)
├── ETR_Summary.xlsx      (cùng nội dung ETR_Summary.pdf, dạng bảng tính lọc/sort được)
└── Evidence/
    └── {id}_{tên file gốc}.{ext}   (chỉ có nếu evidence file tồn tại thật trên đĩa)
```

### 2.3 Để test đủ cả 3 thành phần (kể cả Evidence file thật)

Nếu ETR chưa có evidence file thật nào trên đĩa (data seed dùng path giả), zip sẽ chỉ có 3 file (thiếu `Evidence/`). Muốn có file evidence thật:
```
POST /api/evidences/upload   (multipart/form-data)
  EvidenceTypeId, AccountId, SubjectResultId, File (.jpg/.jpeg/.png/.gif/.webp/.pdf)
```
gán đúng `SubjectResultId` thuộc ETR muốn export, rồi gọi lại bước 2.1.

### 2.4 Quyền truy cập

Cả 2 endpoint đều yêu cầu role **Admin** (`[Authorize(Roles = "Admin")]` ở `ExportsController`) — chưa mở rộng cho role khác (VD Training Manager/Management Viewer như mô tả trong FRD gốc) — nằm ngoài phạm vi yêu cầu lần này, cần xác nhận thêm nếu muốn các role khác cũng gọi được.

---

## 3. Đã kiểm chứng bằng cách nào

- `dotnet test` (project `ETR.Application.Tests`): **8/8 pass** — bao gồm cả 2 test mới cho `BuildEtrSummaryExcel`.
- `dotnet build` toàn solution: **0 Error**.
- Chạy app thật (SQL Server Docker cục bộ) + `curl`:
  - Export ETR #1 (chưa có evidence thật) → zip 3 file, log ghi rõ `Evidence file X ... is missing on disk ... skipped`.
  - Upload 1 evidence file `.pdf` thật qua `POST /api/evidences/upload` → export lại → zip đủ 4 file, `diff` xác nhận evidence file trong zip **giống hệt byte-for-byte** bản gốc.
  - Tên file zip đúng chính xác `STU-01_AMT101-C1_Training_Package.zip`.
  - `ETR_Summary.xlsx` xác nhận là file Excel 2007+ hợp lệ (`file` command), nội dung `sharedStrings.xml` chứa đúng student code/class code/course code/status/header cột.

## 4. Rủi ro/việc còn lại

- **Phát hiện nhưng chưa sửa (ngoài phạm vi yêu cầu lần này)**: khi thao tác xoá/sửa 1 entity con (evidence, attendance...) thuộc ETR đã `Completed`/`Locked`, hệ thống throw `ImmutabilityViolationException` — loại exception này **chưa có trong switch của `GlobalExceptionHandler`** (được siết chặt ở batch 2026-07-23), nên hiện vẫn rơi vào nhánh 500 mơ hồ thay vì 400 rõ ràng. Đây là tình huống rất dễ gặp (bất kỳ thao tác sửa nào trên ETR đã khoá) — nên vá cùng đợt error-handling tới.
- **Endpoint chỉ giới hạn role Admin** — FRD gốc có nhắc tới Training Manager/Management Viewer cũng cần xem báo cáo; chưa mở rộng quyền trong lượt này.
- **`Evidence/` folder có thể rỗng** nếu không có evidence file thật nào tồn tại trên đĩa cho ETR đó — đây là hành vi đúng theo thiết kế (graceful skip), không phải lỗi, nhưng auditor cần biết để không hiểu nhầm là thiếu sót hệ thống.
- **Dữ liệu seed hiện tại dùng path evidence giả** (`/evidence/amt101/...`) — không phải file thật, nên export trên dữ liệu seed mặc định sẽ luôn thiếu `Evidence/` cho tới khi có ai upload file thật qua API.

## 5. Files liên quan

| File | Trạng thái |
|---|---|
| `ETR.Application/Services/ExportService.cs` | Sửa — tách PDF, copy evidence thật, đổi tên zip, thêm Excel |
| `ETR.Application/ETR.Application.csproj` | Sửa — thêm `ClosedXML` |
| `ETR.Application.Tests/ETR.Application.Tests.csproj` | Mới |
| `ETR.Application.Tests/Services/ExportServiceTests.cs` | Mới |
| `ETRSystem.slnx` | Sửa — thêm `ETR.Application.Tests` vào solution |
