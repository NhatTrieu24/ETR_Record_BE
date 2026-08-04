# Hoàn thiện các mục High-priority (H3, H5, H7-H10, H12, H13, H22) + Export báo cáo thật + Báo cáo Frontend 04/08 — 2026-08-04

**Ngày thực hiện:** 2026-08-04
**Phạm vi:** `ETR.API/Controllers/{AccountsController,ClassesController,ClassStudentsController,EvidencesController,AttendanceController,AssessmentResultsController,SearchController,ExportsController,DashboardController,ReportsController,EtrController}.cs`; `ETR.API/Middleware/GlobalExceptionHandler.cs`; `ETR.Application/Services/{AccountService,CourseService,EtrService,ExportService}.cs` (+ `ExportService.Reports.cs` mới); `ETR.Application/Interfaces/IExportService.cs`; `ETR.Application/DTOs/{Account/AccountDtos,Course/Requests/*,Export/Requests/ExportRequest}.cs`; `ETR.Infrastructure/Data/{DataSeeder,AppDbContext.Compliance}.cs`.
**Mục tiêu:** `/mpower:code-fix` cho 9 mục High trong `ETR.Documentation/LO_TRINH_HOAN_THIEN_DU_AN.md` (H3, H5, H7, H8, H9, H10, H12, H13, H22) còn mở sau các batch trước, đồng thời xử lý báo cáo mới từ frontend team (`ETR.Documentation/report_20260804.md` — mở quyền đọc GET cho role Audit trên 6 controller).

---

## 1. Báo cáo Frontend 2026-08-04 — mở quyền đọc GET cho Audit

`report_20260804.md` yêu cầu: Audit cần đọc được `AccountsController`, `ClassesController`, `ClassStudentsController`, `EvidencesController`, `AttendanceController`, `AssessmentResultsController` — vai trò kiểm toán cần nhìn thấy dữ liệu để đối chiếu nhưng trước đó bị loại khỏi các action GET này.

**Đã sửa** (chỉ thêm `Audit` vào các action/class-level đọc, KHÔNG đụng tới action ghi):
- `AccountsController.GetAllAccounts`: `Admin,Academic` → `Admin,Academic,Audit`.
- `ClassesController` (class-level): thêm `Audit` (write actions vẫn narrow về `Admin,Academic,TrainingManager`, không đổi).
- `ClassStudentsController` (class-level, chỉ có 1 action GET duy nhất): thêm `Audit`.
- `EvidencesController.GetAll/GetById/Download`: thêm `Audit` (Upload/Verify/Delete không đổi).
- `AttendanceController.GetAllRecords/GetRecordById`: thêm `Audit` (Record/Confirm/Update/Delete không đổi).
- `AssessmentResultsController.GetAllResults/GetById`: thêm `Audit` (Record/Update/Publish/Delete không đổi).

**Verify sống:** Audit gọi GET trên cả 6 controller → 200 tất cả.

---

## 2. H3 — `ImmutabilityViolationException` rơi vào 500 mơ hồ thay vì 400 rõ ràng

`GlobalExceptionHandler.cs` không có nhánh riêng cho `ImmutabilityViolationException` (kế thừa `InvalidOperationException`, đã bị bỏ khỏi switch từ đợt siết lỗi 2026-07-23) — mọi lần chạm bất biến dữ liệu (sửa Evidence/Attendance/AssessmentResult của ETR đã Completed/Locked) đều trả `500` thay vì `400` rõ ràng.

**Đã sửa:** thêm `ImmutabilityViolationException => (400, "Business rule violation", exception.Message)` vào `Classify()`.

**Verify sống:** `DELETE /api/evidences/1` trên ETR#1 đang Completed → `400 {"detail":"Cannot modify EvidenceFile because the related ETRCourseRecord is Completed or Locked."}` (trước đây là `500`).

---

## 3. H5 — Role "Management Viewer" hoàn toàn chưa tồn tại

FRD định nghĩa 1 role view-only cho ban lãnh đạo (chỉ xem Dashboard/Report/Search) nhưng `DataSeeder.cs` chỉ có 7 role, không có role này — muốn cấp quyền "chỉ xem" phải mượn tạm `TrainingManager` (vốn có quyền ghi).

**Đã sửa:**
- Thêm role `ManagementViewer` vào `DataSeeder.cs` — idempotent theo đúng pattern per-item check đã dùng cho `Department` (không chỉ nằm trong nhánh `if (!AnyAsync())` gốc, để database đã seed từ trước vẫn nhận được role mới khi khởi động lại).
- Seed 1 account demo: `management-viewer@etr.com` / `123456`.
- Cấp `ManagementViewer` vào class-level của `DashboardController` và `ReportsController` (cả 2 chỉ có action GET, không rủi ro ghi). `SearchController` vốn đã `[Authorize]` trần (mọi role đã đăng nhập) nên không cần sửa.

**Verify sống:** đăng nhập `management-viewer@etr.com` → `GET /api/dashboard/stats`, `GET /api/reports/summary`, `GET /api/search/classes` đều `200`.

---

## 4. H7 — `Course.ValidityMonths`/`CourseType` không thể cấu hình qua API

Entity `Course` đã có 2 field này từ trước (dùng để tính `ExpiryDate` trong `EtrService.CompleteEtrAsync`), nhưng `CreateCourseRequest`/`UpdateCourseRequest`/`CourseResponse` không expose — không có cách nào set giá trị qua API, nên tính năng "hạn sử dụng chứng chỉ" chưa bao giờ kích hoạt được trong thực tế.

**Đã sửa:** thêm `int? ValidityMonths`, `string? CourseType` (đều optional, không phá API contract cũ) vào cả 3 DTO + map đầy đủ trong `CourseService` (Create/Update/GetAll/GetById).

**Verify sống:** `GET /api/courses/1` trả `"validityMonths":null,"courseType":null` (field đã xuất hiện trong response, sẵn sàng để set qua Create/Update).

---

## 5. H10 — Tìm kiếm nâng cao thiếu chiều lọc theo Khoá học/Giảng viên/Khoảng ngày

`SearchController.SearchEtrs` trước chỉ lọc theo `query` (khớp Status/ID/tên học viên).

**Đã sửa:** thêm 4 query param tuỳ chọn: `courseId`, `instructorId`, `dateFrom`, `dateTo` (lọc theo `IssuedDate ?? CreatedAt`). `query` cũng đổi thành optional để hỗ trợ tìm chỉ bằng filter, không bắt buộc có từ khoá text.

```
GET /api/search/etrs?courseId=1
GET /api/search/etrs?instructorId=5&dateFrom=2026-01-01&dateTo=2026-12-31
```

**Verify sống:** `?courseId=1` trả đúng 2 ETR thuộc khoá AMT-101; `?instructorId=999` (không tồn tại) trả `[]` đúng như kỳ vọng.

---

## 6. H13 — Reopen (mở khoá) chưa đủ để thực sự sửa dữ liệu con của ETR

Đây là mục phức tạp nhất — cần 1 quyết định nghiệp vụ đã nêu rõ trong roadmap trước khi code. **Quyết định:** khi Reopen một ETR đang `Completed`, chuyển `Status` về `"Verified"` (giá trị non-immutable sẵn có trong state machine, đúng gợi ý ban đầu của mục H13) thay vì giữ nguyên `"Completed"` — nhờ vậy Evidence/Attendance/AssessmentResult con tự động sửa được lại (vì `ImmutabilityValidator.IsEtrImmutable` chỉ khoá khi `Status=="Completed" OR IsLocked==true`, không cần thêm ngoại lệ riêng cho entity con). Sau khi sửa xong, phải gọi lại `POST /api/etr/{id}/complete` như bình thường (đã yêu cầu sẵn `Status=="Verified"`) để khoá lại.

**Thách thức kỹ thuật:** `EtrService.UnlockEtrAsync` trước đây CHỈ được phép đổi `IsLocked` (không đổi property nào khác) — đây là điều kiện bắt buộc để `ImmutabilityValidator`'s `IsBeingUnlocked` exception áp dụng cho chính `ETRCourseRecord` (nếu không, đổi luôn `Status` trong cùng transaction sẽ tự làm ném `ImmutabilityViolationException` ngay khi Reopen). Đã sửa `AppDbContext.Compliance.cs`: `IsBeingUnlocked` nay CŨNG chấp nhận đúng 1 kiểu thay đổi Status đi kèm — `"Completed"` → `"Verified"` — mọi thay đổi Status khác đi kèm unlock vẫn bị chặn như cũ.

**Đã sửa:**
- `AppDbContext.Compliance.cs`: nới điều kiện `IsBeingUnlocked` như trên.
- `EtrService.UnlockEtrAsync`: nếu ETR đang `Completed` thì đổi `Status = "Verified"` cùng lúc với `IsLocked = false`; AuditLog ghi rõ cả 2 giá trị cũ/mới.
- Không cần sửa `ImmutabilityValidator.ValidateEtrChildEntity` — vì Status không còn là `"Completed"` sau khi Reopen, `IsEtrImmutable` tự trả `false`, entity con tự động sửa được.

**Verify sống (đầy đủ vòng lặp):**
1. ETR#1 (Completed, Locked) → `POST /api/etr/1/reopen` → `200`, response `status:"Verified", isLocked:false`.
2. `DELETE /api/evidences/1` (evidence thuộc ETR#1) → `204` (trước đây `500`).
3. `POST /api/etr/1/complete` → `400 "Cannot complete ETR. 1 evidence file(s) are not yet Verified."` — xác nhận đã qua đúng điều kiện tiên quyết `Status=="Verified"` (lỗi là do 1 evidence khác chưa Verified, một business rule hợp lệ khác, không phải lỗi trạng thái).

---

## 7. H22 — `AccountsController.GetAccountById` mở rộng cho 6 role, lộ `Username`/`RoleId`/`DepartmentId`

Sau batch trước, `GetAccountById` đã scope Instructor (chỉ thấy học viên lớp mình phụ trách). Còn lại: TrainingManager — vai trò giám sát đào tạo, không có căn cứ FRD nào cho việc xem `RoleId`/`DepartmentId` của tài khoản bất kỳ (khác QA/Audit, vốn là vai trò kiểm tra/kiểm toán cần full visibility).

**Đã sửa:** `AccountResponse.RoleId`/`DepartmentId` đổi thành `int?` (không phá API contract — Admin/Academic/QA/Audit/Instructor vẫn nhận giá trị thật). Khi caller là `TrainingManager`, `AccountService.GetAccountByIdAsync` trả `null` cho 2 field này, giữ nguyên `AccountId`/`Username`/`Status`/`IsActive` (tra cứu tên hiển thị vẫn hợp lệ).

**Verify sống:** TrainingManager tra `GET /api/accounts/4` → `roleId:null, departmentId:null`; Admin tra cùng ID → đầy đủ `roleId:4, departmentId:1`.

---

## 8. H8 + H9 + H12 — Export báo cáo: từ mock sang thật

Trước đây `ExportsController.ExportPdf`/`ExportDashboard` gọi `CreateMockExportJob` — không sinh file thật, tải xuống chỉ trả 1 đoạn text placeholder. Không có loại export riêng cho Điểm danh/Đánh giá (H9), và không có báo cáo Excel tổng hợp nhiều học viên trong 1 lớp (H12, khác với `BuildEtrSummaryExcel` vốn chỉ cho 1 ETR/học viên, nhúng trong zip Training Package riêng của học viên đó).

**Đã thêm 5 method mới trong `IExportService`** (file mới `ExportService.Reports.cs`, tái dùng đúng pattern QuestPDF/ClosedXML + ghi `ExportJob` đã có sẵn từ `ExportTrainingPackageAsync`):

| Endpoint | ExportType | Nội dung |
|---|---|---|
| `POST /api/exports/pdf` (`etrCourseRecordId`) | `PDF` | PDF tóm tắt 1 ETR độc lập (tái dùng `BuildEtrSummaryPdf`, không đóng gói trong zip) |
| `POST /api/exports/dashboard` | `Dashboard` | PDF tóm tắt KPI dashboard (Total ETRs, Completion Rate, Pending Approval, v.v. — tái dùng `DashboardKpiCalculator`) |
| `POST /api/exports/attendance` (`classId`) | `AttendanceReport` | Excel điểm danh toàn bộ học viên trong 1 lớp — 1 dòng/bản ghi điểm danh |
| `POST /api/exports/assessment` (`classId`) | `AssessmentReport` | Excel kết quả đánh giá toàn bộ học viên trong 1 lớp — 1 dòng/bản ghi đánh giá |
| `POST /api/exports/class-summary` (`classId`) | `ClassSummary` | Excel tổng hợp — 1 dòng/học viên trong lớp (mã, tên, trạng thái ETR, ngày cấp/hết hạn) — chính là mục H12 |

`ExportRequest` DTO thêm field `ClassId` (optional) để phục vụ 3 endpoint mới. Đã xoá `CreateMockExportJob` (không còn dùng) và cập nhật `DownloadExportFile`'s fallback: mọi loại export giờ đều có file thật trên đĩa, nên fallback chỉ còn ý nghĩa "file bị mất trên đĩa ngoài luồng" (trả `404`), không còn nhánh "loại export vẫn mock".

**Verify sống:** cả 5 endpoint trả `200` với `ExportJob` thật; tải xuống cả 5 (`GET /api/exports/download/{id}`) xác nhận bằng lệnh `file`: 2 PDF hợp lệ (`PDF document, version 1.4`), 3 file Excel hợp lệ (`Microsoft Excel 2007+`), không còn nội dung text placeholder.

---

## 9. Đã kiểm chứng bằng cách nào

- `dotnet build` toàn solution: **0 Error** (2 warning có sẵn từ trước, không liên quan).
- `dotnet test` (`ETR.Application.Tests`): **23/23 pass**, không có regression.
- Chạy app thật (Docker SQL Server dev), đăng nhập đủ 8 role (Admin/Instructor/QA/Academic/TrainingManager/Student/Audit/ManagementViewer), `curl` trực tiếp từng endpoint — chi tiết per-mục ở trên.
- H13 verify đặc biệt kỹ vì là fix rủi ro cao nhất: xác nhận cả vòng Reopen → sửa entity con → Complete lại, không chỉ dừng ở Reopen thành công.

## 10. Rủi ro/việc còn lại

- **H8/H9/H12 export mới chưa có unit test** — theo cùng khoảng trống đã ghi nhận ở M18 (logic scoping Instructor cũng thiếu test); nên gộp chung 1 lượt viết test cho `ExportService` mở rộng.
- **H13**: quyết định "Reopen → Verified" là quyết định nghiệp vụ tôi đưa ra dựa trên gợi ý sẵn có trong roadmap (không có FRD owner xác nhận trực tiếp) — nên xác nhận lại với đội nghiệp vụ nếu luồng "reopen xong không cần Complete lại ngay, để draft" cũng cần hỗ trợ (hiện tại ETR ở trạng thái Verified sau reopen vẫn hợp lệ để dừng ở đó không Complete ngay, không bị ép Complete ngay lập tức).
- **ManagementViewer**: mới có 3 endpoint được cấp quyền (Dashboard/Reports/Search) theo đúng phạm vi roadmap nêu — nếu FE cần thêm màn hình view-only khác (VD xem danh sách Class/Course), cần bổ sung role này vào controller tương ứng khi có yêu cầu cụ thể.
- Các mục còn lại trong roadmap (H2, H11, M-series, L-series) không thuộc phạm vi yêu cầu lần này — không đụng tới.

## 11. Files liên quan

| File | Trạng thái |
|---|---|
| `ETR.API/Controllers/AccountsController.cs` | Sửa — GetAllAccounts thêm Audit |
| `ETR.API/Controllers/ClassesController.cs` | Sửa — class-level thêm Audit |
| `ETR.API/Controllers/ClassStudentsController.cs` | Sửa — class-level thêm Audit |
| `ETR.API/Controllers/EvidencesController.cs` | Sửa — 3 action GET thêm Audit |
| `ETR.API/Controllers/AttendanceController.cs` | Sửa — 2 action GET thêm Audit |
| `ETR.API/Controllers/AssessmentResultsController.cs` | Sửa — 2 action GET thêm Audit |
| `ETR.API/Controllers/DashboardController.cs` | Sửa — class-level thêm ManagementViewer |
| `ETR.API/Controllers/ReportsController.cs` | Sửa — class-level thêm ManagementViewer |
| `ETR.API/Controllers/SearchController.cs` | Sửa — SearchEtrs thêm 4 filter param |
| `ETR.API/Controllers/EtrController.cs` | Sửa — cập nhật XML doc ReopenEtr |
| `ETR.API/Controllers/ExportsController.cs` | Sửa — 3 endpoint export mới, bỏ mock, thêm `ResolveWebRootPath` |
| `ETR.API/Middleware/GlobalExceptionHandler.cs` | Sửa — thêm nhánh `ImmutabilityViolationException` |
| `ETR.Application/Services/AccountService.cs` | Sửa — slim response cho TrainingManager |
| `ETR.Application/Services/CourseService.cs` | Sửa — map ValidityMonths/CourseType |
| `ETR.Application/Services/EtrService.cs` | Sửa — UnlockEtrAsync đổi Status khi Reopen |
| `ETR.Application/Services/ExportService.cs` | Sửa — đổi `partial class` |
| `ETR.Application/Services/ExportService.Reports.cs` | Mới — 5 method export + helper dùng chung |
| `ETR.Application/Interfaces/IExportService.cs` | Sửa — thêm 5 khai báo method |
| `ETR.Application/DTOs/Account/AccountDtos.cs` | Sửa — `RoleId`/`DepartmentId` thành nullable |
| `ETR.Application/DTOs/Course/Requests/CreateCourseRequest.cs` | Sửa — thêm ValidityMonths/CourseType |
| `ETR.Application/DTOs/Course/Requests/UpdateCourseRequest.cs` | Sửa — thêm ValidityMonths/CourseType |
| `ETR.Application/DTOs/Course/Responses/CourseResponse.cs` | Sửa — thêm ValidityMonths/CourseType |
| `ETR.Application/DTOs/Export/Requests/ExportRequest.cs` | Sửa — thêm `ClassId` |
| `ETR.Infrastructure/Data/DataSeeder.cs` | Sửa — thêm role + account ManagementViewer |
| `ETR.Infrastructure/Data/AppDbContext.Compliance.cs` | Sửa — nới điều kiện `IsBeingUnlocked` |
| `ETR.Documentation/LO_TRINH_HOAN_THIEN_DU_AN.md` | Sửa — đánh dấu các mục đã fix |
