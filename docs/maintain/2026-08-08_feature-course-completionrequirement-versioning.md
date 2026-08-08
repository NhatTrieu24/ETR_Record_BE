# Feature: Versioning cho Course & CompletionRequirement — 2026-08-08

**Ngày thực hiện:** 2026-08-08
**Phạm vi:** `ETR.Domain/Entities/{Course,CompletionRequirement,ETRCourseRecord}.cs` (mở rộng); `ETR.Infrastructure/Migrations/*_AddCourseAndCompletionRequirementVersioning.cs` (mới); `ETR.Application/DTOs/Course/Responses/CourseResponse.cs`, `ETR.Application/DTOs/CompletionRequirement/CompletionRequirementResponse.cs` (mở rộng); `ETR.Application/Services/{CourseService,CompletionRequirementService,EnrollmentService,EtrService}.cs` (mở rộng); `ETR.API/Controllers/CompletionRequirementsController.cs` (doc comment); test mới `ETR.Application.Tests/Services/{CourseServiceTests,CompletionRequirementServiceTests}.cs`, mở rộng `EtrServiceTests.cs`, `EnrollmentServiceTests.cs`.
**Mục tiêu:** `/mpower:code-fix` — triển khai mục #3 trong `docs/todo/9.todo_to_complete_system.md` (Versioning cho Course & CompletionRequirement), giải quyết đúng scenario concern.md đã nêu: nâng chuẩn chuyên cần giữa khóa (VD: 80% → 90%) khiến học viên đã tốt nghiệp trước đó bị đối chiếu lại theo luật mới, rớt hồi tố oan.

---

## 1. Vấn đề trước khi sửa

`Course` và `CompletionRequirement` là dữ liệu mutable thuần túy — `PUT` ghi đè trực tiếp lên đúng 1 row. Checklist Validation (`EtrService.SubmitEtrAsync`, `GetCompletionProgressAsync`) luôn query "CompletionRequirement hiện có" tại **thời điểm gọi API**, không phải tại thời điểm học viên Enroll — nên sửa `ThresholdValue` giữa khóa lập tức ảnh hưởng đến MỌI ETR đang tồn tại, kể cả những ETR đã hoàn tất từ trước và đang được tra cứu lại (audit, thanh tra CAA).

Rà soát thêm cho thấy `Course.ValidityMonths` **không thực sự có rủi ro hồi tố** — `ExpiryDate` được tính và lưu (snapshot) đúng 1 lần tại `CompleteEtrAsync`, không bao giờ tính lại sau đó. Rủi ro thật nằm hoàn toàn ở `CompletionRequirement`, vì đây là bảng được query **sống** (live) mỗi lần Submit/xem tiến độ.

## 2. Thiết kế

### 2.1 Course.VersionNo — bộ đếm chung, không nhân bản row

Khác với cách tiếp cận "mỗi version là 1 row mới" (sẽ kéo theo phải nhân bản CourseSubject/Assessment/PracticalChecklist cho từng version — blast radius rất lớn, không cần thiết cho đúng rủi ro đang giải quyết), `Course` giữ nguyên `CourseId` ổn định, chỉ thêm **bộ đếm** `VersionNo` (mặc định 1) + `EffectiveFrom` (khi version hiện tại có hiệu lực). `VersionNo` chỉ tăng khi `ValidityMonths` đổi (trường duy nhất hiện có logic đọc) hoặc khi 1 `CompletionRequirement` con của Course đó được version hóa (xem 2.2) — nó đóng vai trò "bộ đếm version chung của cả Course" để `ETRCourseRecord.CourseVersionNo` có 1 con số duy nhất, ổn định để snapshot.

### 2.2 CompletionRequirement — append-only khi đổi field ảnh hưởng kết quả

Đây là phần chịu trách nhiệm chính cho đúng rủi ro trong concern.md. `CompletionRequirement` được thêm `VersionNo`, `EffectiveFrom`, `EffectiveTo`. Khi `PUT /api/completionrequirements/{id}` đổi `RequirementType`, `ThresholdValue`, hoặc `IsMandatory` (3 field duy nhất `EtrService` thực sự dùng để tính Pass/Fail):

1. Đóng row cũ: `EffectiveTo = now` (KHÔNG xóa, KHÔNG ghi đè — vẫn còn nguyên để tra cứu lịch sử/audit).
2. Bump `Course.VersionNo += 1`.
3. Tạo row MỚI: `RequirementId` mới, cùng `CourseId`, field đã cập nhật, `VersionNo = Course.VersionNo` (mới).

Nếu chỉ đổi field mô tả (`RequirementName`/`Description`/`DisplayOrder`) → vẫn ghi đè tại chỗ như cũ, không version hóa — tránh làm phình version number vì lỗi chính tả không ảnh hưởng đánh giá.

**Hệ quả cần FE lưu ý**: `PUT` giờ có thể trả về `RequirementId` KHÁC với `{id}` trong URL khi thay đổi thuộc loại ảnh hưởng kết quả — đã ghi rõ trong XML doc comment của controller.

### 2.3 ETRCourseRecord.CourseVersionNo — snapshot tại thời điểm Enroll

`EnrollmentService.CreateEnrollmentAsync` giờ đọc `Course.VersionNo` tại thời điểm Enroll và lưu vào `ETRCourseRecord.CourseVersionNo`. Đây là con số **bất biến** theo hồ sơ đó — không bao giờ đổi sau khi tạo, kể cả khi Course/CompletionRequirement được version hóa nhiều lần sau đó.

### 2.4 Checklist Validation lọc theo snapshot, không theo bản mới nhất

`EtrService.SubmitEtrAsync` và `GetCompletionProgressAsync` — dòng lọc `CompletionRequirement`:

```diff
- .Where(cr => cr.CourseId == trainingClass.CourseId && cr.IsMandatory)
+ .Where(cr => cr.CourseId == trainingClass.CourseId && cr.IsMandatory && cr.VersionNo == etr.CourseVersionNo)
```

Học viên Enroll khi luật còn 80% (`VersionNo=1`) mãi mãi được đối chiếu với row `VersionNo=1` (vẫn còn nguyên trong DB, chỉ bị đóng `EffectiveTo`) — không bị ảnh hưởng bởi row `VersionNo=2` (90%) tạo ra sau đó cho học viên enroll mới.

### 2.5 `GetCompletionRequirementsByCourseAsync` — chỉ trả bản đang hiệu lực

Endpoint `GET /completionrequirements/course/{courseId}` (dùng cho màn hình cấu hình khóa học) giờ lọc `EffectiveTo == null` — chỉ hiển thị luật ĐANG áp dụng, không lẫn lộn với các bản cũ đã bị supersede. Muốn xem lịch sử đầy đủ (kể cả bản cũ) vẫn dùng `GET /completionrequirements` (tất cả) hoặc `GET /completionrequirements/{id}` (theo đúng RequirementId lịch sử).

## 3. Business rules đã enforce

1. `Course.VersionNo` chỉ bump khi `ValidityMonths` đổi — các field mô tả khác (tên, mô tả, thời lượng, trạng thái, loại khóa) vẫn ghi đè tại chỗ.
2. `CompletionRequirement` chỉ version hóa khi `RequirementType`/`ThresholdValue`/`IsMandatory` đổi — cosmetic fields ghi đè tại chỗ.
3. Row `CompletionRequirement` cũ KHÔNG BAO GIỜ bị xóa hay ghi đè sau khi bị supersede — chỉ đóng `EffectiveTo`, phục vụ audit/thanh tra CAA truy vết đúng luật đã áp dụng tại từng thời điểm.
4. `ETRCourseRecord.CourseVersionNo` là snapshot bất biến tại Enroll — không tự cập nhật lại kể cả khi Course version tăng sau đó.
5. Checklist Validation LUÔN đối chiếu theo `CourseVersionNo` đã snapshot, không theo bản `CompletionRequirement` mới nhất.

## 4. Việc CHƯA làm / rủi ro đã biết

- **CourseSubject và Assessment KHÔNG được version hóa** — nằm ngoài phạm vi mục #3 trong todo doc (chỉ nêu đích danh Course & CompletionRequirement). Nếu sau này phát hiện rủi ro tương tự ở `CourseSubject.PassingScore`/`Assessment.PassingScore`, cần một lượt versioning riêng theo đúng pattern đã áp dụng ở đây.
- **Chưa đồng bộ 3 file raw SQL deploy thủ công** (`Deploy_ETR_System.sql`, `ALL_IN_ONE_Deploy.sql`, `Deploy_NukeAndSeed.sql`) với các cột mới — chỉ migration EF Core đã có. Cần bổ sung cột trước khi dùng các file này để reset DB thủ công (cùng loại rủi ro đã ghi nhận ở migration Amendment ngày 2026-08-08 trước đó).
- **`PUT /completionrequirements/{id}` đổi RequirementId khi thay đổi ảnh hưởng kết quả** — vi phạm kỳ vọng REST thông thường (PUT thường giữ nguyên ID). Đây là đánh đổi có chủ đích để đạt đúng yêu cầu "không ghi đè" của todo doc; FE cần cập nhật để đọc `RequirementId` từ response thay vì giữ id cũ sau khi gọi PUT.
- **`GetAllCoursesAsync`/`GetCourseByIdAsync` không lọc theo version** — vì Course không nhân bản row (chỉ 1 CourseId, VersionNo là bộ đếm trên chính row đó), không có "nhiều bản Course" để lọc; hành vi giữ nguyên như cũ, chỉ thêm field `VersionNo` để hiển thị.

## 5. Đã kiểm chứng bằng cách nào

- **TDD**: viết test trước cho từng nhánh hành vi (Course: đổi ValidityMonths → bump VersionNo + AuditLog; không đổi → không bump; CompletionRequirement: đổi Threshold → đóng row cũ + tạo row mới versioned; chỉ đổi cosmetic → ghi đè tại chỗ; GetByCourse chỉ trả bản hiệu lực; EtrService: GetCompletionProgressAsync đối chiếu đúng theo `CourseVersionNo` đã snapshot, bỏ qua bản mới hơn dù khắt khe hơn) → xác nhận RED (build lỗi vì field/method chưa tồn tại) → implement → xác nhận GREEN.
- `dotnet build` toàn solution: **0 Error** (2 warning pre-existing, không liên quan).
- `dotnet ef migrations add AddCourseAndCompletionRequirementVersioning`: tạo migration; **đã tự sửa tay** giá trị mặc định EF Core sinh ra ban đầu (`defaultValue: 0` cho các cột `VersionNo`/`CourseVersionNo`) thành `defaultValue: 1` để dữ liệu cũ backfill đúng với quy ước "version 1" mà code C# dùng — nếu không sửa, dữ liệu cũ sẽ mang giá trị 0 trong khi mọi bản ghi mới tạo từ nay mang giá trị 1, dễ gây nhầm lẫn khi debug dù không gây sai logic (so khớp vẫn đúng nội bộ 0==0). Build lại sau khi sửa: **0 Error**.
- `dotnet test`: **53/53 pass** (47 cũ + 6 mới), không regression. Test đáng chú ý: `GetCompletionProgressAsync_UsesCompletionRequirementMatchingEtrCourseVersionNo_NotTheLatestOne` — dựng 1 ETR có `CourseVersionNo=1` (ngưỡng chuyên cần 80%), 1 CompletionRequirement mới hơn `VersionNo=2` (ngưỡng 90%), học viên đạt 85% — xác nhận hệ thống chấm ĐẠT theo luật 80% cũ, không bị đối chiếu nhầm theo luật 90% mới hơn.

## 6. Files liên quan

| File | Trạng thái |
|---|---|
| `ETR.Domain/Entities/Course.cs` | Sửa — thêm `VersionNo`, `EffectiveFrom` |
| `ETR.Domain/Entities/CompletionRequirement.cs` | Sửa — thêm `VersionNo`, `EffectiveFrom`, `EffectiveTo` |
| `ETR.Domain/Entities/ETRCourseRecord.cs` | Sửa — thêm `CourseVersionNo` |
| `ETR.Infrastructure/Migrations/20260808051722_AddCourseAndCompletionRequirementVersioning.cs` (+ `.Designer.cs`) | Mới — đã tay chỉnh `defaultValue` |
| `ETR.Application/DTOs/Course/Responses/CourseResponse.cs` | Sửa — thêm `VersionNo` |
| `ETR.Application/DTOs/CompletionRequirement/CompletionRequirementResponse.cs` | Sửa — thêm `VersionNo`, `EffectiveFrom`, `EffectiveTo` |
| `ETR.Application/Services/CourseService.cs` | Sửa — bump `VersionNo` khi `ValidityMonths` đổi |
| `ETR.Application/Services/CompletionRequirementService.cs` | Sửa — append-only versioning khi field ảnh hưởng kết quả đổi; `GetByCourse` lọc `EffectiveTo == null` |
| `ETR.Application/Services/EnrollmentService.cs` | Sửa — snapshot `CourseVersionNo` tại Enroll |
| `ETR.Application/Services/EtrService.cs` | Sửa — lọc `CompletionRequirement` theo `CourseVersionNo` ở `SubmitEtrAsync` và `GetCompletionProgressAsync` |
| `ETR.API/Controllers/CompletionRequirementsController.cs` | Sửa — doc comment cảnh báo PUT có thể đổi RequirementId |
| `ETR.Application.Tests/Services/CourseServiceTests.cs` | Mới — 2 test case |
| `ETR.Application.Tests/Services/CompletionRequirementServiceTests.cs` | Mới — 3 test case |
| `ETR.Application.Tests/Services/EtrServiceTests.cs` | Sửa — thêm 1 test case |
| `ETR.Application.Tests/Services/EnrollmentServiceTests.cs` | Sửa — mock thêm `CourseRepository` (test cũ bị lỗi NRE sau khi thêm bước fetch Course) |
| `docs/todo/9.todo_to_complete_system.md`, `ETR.Documentation/final/9.todo_to_complete_system.md` | Sửa — đánh dấu mục #3 đã triển khai |
