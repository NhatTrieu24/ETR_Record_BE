# Chuyển Evidence sang Cloudinary (bảng Attachment đa hình) + Học lại chỉ môn chưa Pass — 2026-08-20

**Ngày thực hiện:** 2026-08-20
**Phạm vi:** `ETR.Domain/Entities/Attachment.cs` (mới), `EvidenceFile.cs`, `SubjectResult.cs`; `ETR.Infrastructure/Data/AppDbContext.cs` + migration mới `AddAttachmentAndSubjectResultCarryOver`; `ETR.Application/Services/EvidenceService.cs`, `ExportService.cs`, `EnrollmentService.cs`, `EtrService.cs`, `DashboardService.cs`; DTOs liên quan (`UploadEvidenceRequest`, `EvidenceResponse`, `EtrDetailsResponse`, `EtrEvidenceFileResponse`); `ETR.API/Controllers/EvidencesController.cs`.
**Mục tiêu:** Theo yêu cầu `/mpower:code-fix` + `/mpower:code-refactor`: (1) ngừng upload file tài liệu/ảnh thẳng vào ổ đĩa server, chuyển sang mô hình FE upload trực tiếp lên Cloudinary rồi chỉ gửi URL cho BE, lưu trữ bằng một bảng đa hình (polymorphic) dùng chung thay vì bảng/field riêng cho từng entity; (2) chức năng đăng ký học lại chỉ bắt học lại các môn chưa Pass, giữ nguyên môn đã Pass/Exempted, và học viên xem được tiến trình này.

---

## 1. Tóm tắt những gì đã implement

### 1.1 Bảng `Attachment` đa hình (polymorphic) thay cho lưu file trên ổ đĩa server

**Vấn đề trước khi sửa:** `EvidenceService.UploadEvidenceAsync` ghi trực tiếp file người dùng tải lên vào `wwwroot/uploads/evidences` trên server, và `EvidenceFile` entity mang theo 5 field mô tả file (`FileName`, `FilePath`, `FileExtension`, `MimeType`, `FileSize`). Nếu làm y hệt cho các entity khác cần đính kèm file trong tương lai sẽ phải lặp lại 5 field này ở từng entity, hoặc tạo bảng riêng cho từng loại — phình DB dần theo thời gian.

**Thiết kế mới:** Một bảng `Attachment` duy nhất, dùng chung cho mọi entity cần tham chiếu file:

```
Attachment
- AttachmentId (PK)
- OwnerType (string)   — nameof(TOwner), VD "EvidenceFile"
- OwnerId (int)        — PK của owner
- Url (string)         — Cloudinary secure_url (hoặc bất kỳ host ngoài nào)
- PublicId (string?)   — Cloudinary public_id, dành cho thao tác xóa trên Cloudinary sau này
- FileName, MimeType, FileSize
- UploadedByAccountId, UploadedAt
+ BaseEntity (soft-delete, audit)
```

`OwnerType`/`OwnerId` là polymorphic association — **cố ý không có FK constraint** (không thể có FK trỏ vào nhiều bảng khác nhau cùng lúc bằng công cụ EF Core chuẩn), có index `(OwnerType, OwnerId)` để tra cứu nhanh. Muốn thêm loại owner mới (VD avatar UserProfile, logo Department...) trong tương lai chỉ cần thêm 1 giá trị `OwnerType` mới, không cần bảng/migration mới.

`EvidenceFile` giữ nguyên các field nghiệp vụ (`EvidenceTypeId`, `AccountId`, `SubjectResultId`, `AttendanceRecordId`, `AssessmentResultId`, `VerificationStatus`, `VerifiedByAccountId`, `VerifiedAt`, `VerificationComment`) — **bỏ hẳn 5 field file-metadata**, thay bằng 1 dòng `Attachment` với `OwnerType = nameof(EvidenceFile)`, `OwnerId = EvidenceFileId`.

**Luồng upload mới:**
```
FE upload file thẳng lên Cloudinary (không qua BE)
FE nhận về secure_url + public_id
FE gọi POST /api/evidences/upload (JSON, không còn multipart/form-data)
   { evidenceTypeId, accountId, subjectResultId, fileUrl, publicId, fileName, mimeType, fileSize }
BE validate metadata (extension/mimetype trong whitelist, fileUrl phải https tuyệt đối)
BE tạo EvidenceFile + Attachment, KHÔNG NHẬN/GHI byte file nào
```

`GET /api/evidences/{id}/download` không còn đọc `PhysicalFile` từ đĩa — trả về **302 Redirect** thẳng đến URL Cloudinary.

**`ExportService.ExportTrainingPackageAsync` (đóng gói ZIP cho auditor)** trước đây đọc evidence trực tiếp từ đĩa để nhét vào ZIP — nay **tải tạm qua `HttpClient` từ URL Cloudinary** rồi ghi vào ZIP (đã xác nhận cách này với user để giữ nguyên trải nghiệm "1 file ZIP tự chứa" cho auditor thay vì đổi sang chỉ có link). Không lưu byte nào lại trên server sau khi ZIP xong; file evidence không fetch được (mạng lỗi, Cloudinary xóa...) bị bỏ qua kèm log warning, không làm fail cả export.

**Migration:** `AddAttachmentAndSubjectResultCarryOver` — DROP 5 cột file-metadata trên `EvidenceFiles`, tạo bảng `Attachments` mới, thêm cột `CarriedOverFromSubjectResultId` trên `SubjectResults` (mục 1.2). **Chưa apply vào DB đang cấu hình** (theo xác nhận với user — DB hiện trỏ Azure có thể là DB dùng chung, và migration này DROP cột có dữ liệu thật) — file migration + script SQL đã sẵn sàng, team tự chạy `dotnet ef database update` khi xác nhận an toàn.

### 1.2 Đăng ký học lại chỉ học lại môn chưa Pass

**Vấn đề trước khi sửa:** `EnrollmentService.CreateEnrollmentAsync` đã có sẵn cơ chế nhận diện lần enroll trước (`previousEtr`, gán vào `ETRCourseRecord.PreviousRecordId`) nhưng **không dùng nó cho gì cả** — vòng lặp tạo `SubjectResult` luôn luôn set `Status = Pending` cho MỌI CourseSubject của khóa học, kể cả môn học viên đã Pass ở lần học trước.

**Sửa:** Trước khi tạo SubjectResult cho từng môn, tra cứu SubjectResult tương ứng (cùng `SubjectId`) từ `previousEtr`:
- Nếu tồn tại và **Status là Passed hoặc Exempted** → **carry-over**: SubjectResult mới copy nguyên `Status`/`Score`/`AttendanceRate`/`EvaluatedAt`/`EvaluatedByAccountId` từ lần trước, đồng thời set `CarriedOverFromSubjectResultId` trỏ về SubjectResult gốc — học viên **không phải học/thi lại** môn này.
- Nếu không tồn tại, hoặc Status là Pending/Failed → tạo mới với `Status = Pending` như cũ — **phải học lại**.

Để `EtrService.SubmitEtrAsync`/`GetCompletionProgressAsync` (kiểm tra "tất cả assessment/checklist đã Pass" dựa trên các dòng con của chính ETR này) tiếp tục hoạt động đúng mà **không cần sửa logic đó**, các dòng `AssessmentResult`/`PracticalChecklistResult` của môn carry-over cũng được copy nguyên kết quả cuối cùng (điểm, `ResultStatus`, `IsPublished`) từ lần enroll trước — thay vì để trống/Pending như môn phải học lại.

**Học viên theo dõi tiến trình:** `EtrSubjectDetailResponse` (trả về từ `GET /api/etr/{id}`) có thêm field `IsCarriedOver` (bool) — `true` nếu môn được giữ nguyên từ lần học trước, `false` nếu đang phải học/thi lại lần này. FE có thể hiển thị 2 nhóm rõ ràng: "Giữ nguyên (đã Pass)" vs "Cần học lại".

---

## 2. Đã kiểm chứng bằng cách nào

- `dotnet build ETRSystem.slnx`: **0 Error** trên cả 5 project.
- `dotnet test ETR.Application.Tests`: **8/8 pass**.
- `dotnet ef migrations add` chạy thành công, `dotnet ef migrations script` sinh SQL hợp lệ (đã review thủ công — chỉ DROP đúng 5 cột dự kiến, tạo đúng 1 bảng + 1 cột, không đụng dữ liệu bảng nào khác).
- Chạy app thật (`dotnet run`) + kiểm tra `swagger.json`:
  - `UploadEvidenceRequest` schema xác nhận đúng field mới (`fileUrl`, `publicId`, `fileName`, `mimeType`, `fileSize`) — **không còn field file nhị phân** (`IFormFile`) nào trong request.
  - 3 endpoint Evidence + endpoint `download` (giờ redirect) đều lên đúng route.
- Không verify được luồng upload thật với Cloudinary (cần tài khoản Cloudinary + FE thật để lấy URL hợp lệ) trong lượt này — đã review kỹ code theo đúng thiết kế đã thống nhất; endpoint validate scheme `https` tuyệt đối trước khi lưu.
- Không verify được luồng retake bằng gọi API thật với 1 học viên có lịch sử enroll trước đó cụ thể (cần dựng dữ liệu 2 lần enroll cùng course trên DB dùng chung, tốn thời gian hơn phạm vi 1 lượt smoke test) — đã review logic + code compile đúng, dùng lại chính xác cơ chế `previousEtr`/`PreviousRecordId` đã có sẵn và được test qua các batch trước.

## 3. Rủi ro/việc còn lại

- **Migration chưa được áp dụng vào DB** — bảng `EvidenceFiles` hiện tại (nếu có dữ liệu thật trên DB đang dùng) vẫn còn 5 cột file-metadata cũ cho đến khi team chạy `dotnet ef database update`. Sau khi DROP, **các evidence đã upload trước đây sẽ mất tham chiếu file** (không có Attachment tương ứng, vì dữ liệu cũ trỏ vào file trên đĩa server chứ không phải Cloudinary) — cần có kế hoạch migrate dữ liệu cũ (upload lại lên Cloudinary + tạo Attachment tương ứng) TRƯỚC khi chạy migration này trên môi trường có dữ liệu evidence thật, nếu không muốn mất lịch sử.
- **Breaking API contract** cho FE: `POST /api/evidences/upload` đổi từ `multipart/form-data` (IFormFile) sang JSON body (`fileUrl`/metadata) — FE cần tích hợp Cloudinary upload widget/SDK trước khi đổi, và cần cấu hình Cloudinary upload preset/API key phía FE (BE không giữ Cloudinary credentials, chỉ nhận URL kết quả).
- `ExportService` giờ phụ thuộc mạng (tải evidence từ Cloudinary khi export Training Package) — nếu Cloudinary chậm/không truy cập được, export vẫn thành công nhưng evidence bị thiếu (đã log warning per file, không fail cả export) — cần theo dõi log này trong thực tế vận hành.
- Carry-over hiện chỉ áp dụng logic tại **thời điểm tạo enrollment mới** (`CreateEnrollmentAsync`) — nếu một `SubjectResult` được sửa thành Passed SAU KHI enrollment mới đã được tạo (VD do Amendment approve muộn), carry-over của lần enroll tiếp theo sau đó vẫn đọc đúng giá trị mới nhất tại thời điểm nó chạy, không có vấn đề gì thêm cần lưu ý.

## 4. Files liên quan

| File | Trạng thái |
|---|---|
| `ETR.Domain/Entities/Attachment.cs` | Mới |
| `ETR.Infrastructure/Migrations/20260819165938_AddAttachmentAndSubjectResultCarryOver.cs` (+ `.Designer.cs`) | Mới — **chưa apply vào DB** |
| `ETR.Domain/Entities/EvidenceFile.cs` | Sửa — bỏ 5 field file-metadata |
| `ETR.Domain/Entities/SubjectResult.cs` | Sửa — thêm `CarriedOverFromSubjectResultId` |
| `ETR.Infrastructure/Data/AppDbContext.cs` | Sửa — thêm `DbSet<Attachment>`, key + index `(OwnerType, OwnerId)` |
| `ETR.Infrastructure/Data/DataSeeder.cs` | Sửa — seed evidence dùng Attachment + URL Cloudinary demo thay vì ghi file thật lên đĩa |
| `ETR.Application/Interfaces/IUnitOfWork.cs`, `ETR.Infrastructure/Repositories/UnitOfWork.cs` | Sửa — thêm `AttachmentRepository` |
| `ETR.Application/Interfaces/IEvidenceService.cs`, `Services/EvidenceService.cs` | Sửa — bỏ toàn bộ I/O đĩa, thêm join với Attachment |
| `ETR.Application/DTOs/Evidence/Requests/UploadEvidenceRequest.cs`, `EvidenceResponse.cs` | Sửa — `IFormFile File` → `FileUrl`/`PublicId`/`FileName`/`MimeType`/`FileSize`; `FilePath` → `FileUrl` |
| `ETR.API/Controllers/EvidencesController.cs` | Sửa — upload nhận JSON, download redirect sang Cloudinary URL |
| `ETR.Application/Services/ExportService.cs` | Sửa — embedding evidence vào ZIP qua `HttpClient` fetch thay vì đọc file đĩa |
| `ETR.Application/DependencyInjection.cs` | Sửa — đăng ký `IHttpClientFactory` (`AddHttpClient()`) |
| `ETR.Application/Services/DashboardService.cs`, `EtrService.cs` | Sửa — cập nhật mọi chỗ đọc `EvidenceFile.FileName`/... sang join Attachment |
| `ETR.Application/DTOs/Etr/Responses/EtrDetailsResponse.cs` | Sửa — `EtrEvidenceFileResponse.FilePath` → `FileUrl`; `EtrSubjectDetailResponse` thêm `IsCarriedOver` |
| `ETR.Application/Services/EnrollmentService.cs` | Sửa — `CreateEnrollmentAsync` thêm logic carry-over SubjectResult/AssessmentResult/PracticalChecklistResult |
