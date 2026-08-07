# Feature: Certificate Validity — Grounded Status & Recurrent Training — 2026-08-07

**Ngày thực hiện:** 2026-08-07
**Phạm vi:** `ETR.Application/Compliance/{LearnerStatus,CertificateValidityCalculator}.cs` (mới); `ETR.Application/DTOs/Etr/Responses/GroundedStatusRefreshResponse.cs` (mới); `ETR.Application/Interfaces/IEtrService.cs` (mở rộng); `ETR.Application/Services/EtrService.cs` (mở rộng); `ETR.Application/Services/EnrollmentService.cs` (mở rộng); `ETR.API/Controllers/EtrController.cs` (2 endpoint mới); test mới `ETR.Application.Tests/Services/{EtrServiceTests,EnrollmentServiceTests}.cs`.
**Mục tiêu:** `/mpower:code-fix` — triển khai mục #4 trong `docs/todo/9.todo_to_complete_system.md` (Certificate Validity, Grace Period, Grounded status, Recurrent Training), đặc thù nghiệp vụ hàng không chưa từng được mô tả ở 8 file business doc gốc trước khi được viết lại.

> **Cập nhật cùng ngày (2026-08-07):** bản đầu tiên clear Grounded ngay khi Enroll lại thành công — mục 4 (Rủi ro/giả định) đã tự nêu rõ đây là điểm cần xác nhận lại vì rủi ro an toàn bay. Đã xác nhận và sửa: Grounded giờ chỉ clear khi ETR mới đó thực sự `Completed` (Training Manager Approve), không phải khi vừa Enroll lại. Toàn bộ nội dung dưới đây đã được cập nhật để phản ánh đúng hành vi cuối cùng — xem mục 3.3 và 7.

---

## 1. Vấn đề trước khi sửa

`EtrService.GetExpiringStudentsAsync` đã có sẵn từ trước (tính `ExpiryDate` từ `Course.ValidityMonths`, trả về `ValidityStatus = Expired/ExpiringSoon`), nhưng chỉ là **endpoint đọc** — không có hành động tự động nào xảy ra khi chứng chỉ thực sự hết hạn. `UserProfile.Status` chỉ có 3 giá trị thực tế được dùng (`Active/Withdrawn/Graduated`), không có khái niệm "không đủ điều kiện làm nhiệm vụ" (Grounded) — đặc thù bắt buộc của ngành hàng không khi nhân sự có chứng chỉ (Safety, First Aid, Sim...) hết hạn mà chưa đào tạo lại.

## 2. Thiết kế

### 2.1 Vì sao không thêm cột `Version`/enum thật cho Status?

`UserProfile.Status` đã là `string` tự do từ trước (không có DB-level enum, không validation allowed-values ở tầng service) — thêm 1 giá trị hợp lệ mới (`Grounded`) không cần migration. Chỉ cần 1 nơi định nghĩa hằng số dùng chung để tránh magic string rải rác — `LearnerStatus` (`ETR.Application/Compliance/LearnerStatus.cs`), theo đúng pattern đã có của `BusinessRuleEngine` trong cùng thư mục `Compliance/`.

### 2.2 Logic "hết hạn" dùng chung — `CertificateValidityCalculator`

Cả việc tự động Grounded (quét toàn hệ thống) và tự động clear Grounded (khi Enroll lại) đều cần trả lời đúng 1 câu hỏi: *"học viên này còn Course nào đang hết hạn chứng chỉ không?"*. Logic này được tách vào 1 static helper dùng chung — `CertificateValidityCalculator.HasAnyExpiredCompletedEtrAsync(IUnitOfWork, accountId, ct)` — để 2 call site (`EtrService`, `EnrollmentService`) không bao giờ lệch nhau về định nghĩa "hết hạn".

Cách tính: với mỗi Course mà học viên đã từng Enroll, lấy `ETRCourseRecord` **gần nhất** của Course đó (sắp theo `IssuedDate ?? CreatedAt`, giảm dần) — nếu record đó có `ExpiryDate` đã qua, coi Course đó là đang hết hạn. Một `ETRCourseRecord` mới tạo ra khi Enroll lại có `ExpiryDate = null` (chưa hoàn thành, chưa có hạn) nên không tính là hết hạn — nhưng nó chỉ thật sự trở thành "record hợp lệ đại diện cho Course đó" theo nghĩa nghiệp vụ khi được `Completed` (có `IssuedDate`/`ExpiryDate` thật). Xem mục 3.3 để biết vì sao việc "trở thành gần nhất" không tự động nghĩa là "đủ điều kiện bay".

### 2.3 3 điểm chạm nghiệp vụ

| # | Điểm chạm | File | Vai trò gọi |
|---|---|---|---|
| 1 | Quét toàn hệ thống, tự Grounded/clear | `POST /api/etr/refresh-grounded-status` | Admin |
| 2 | Xem danh sách cần đào tạo lại | `GET /api/etr/due-for-training?courseId=&daysThreshold=` | Admin, Instructor, QA, TrainingManager, Academic |
| 3 | Tự clear khi ETR mới `Completed` | `EtrService.CompleteEtrAsync` (không có endpoint riêng — xảy ra ngầm trong `POST /api/etr/{id}/complete`) | Tự động, kích hoạt bởi Training Manager/Admin khi Approve |

## 3. Chi tiết implementation

### 3.1 `POST /api/etr/refresh-grounded-status`

Quét tất cả `UserProfile` có `Status ∈ {Active, Grounded}` (bỏ qua `Withdrawn`/`Graduated` — không còn trong pipeline đào tạo, không tự đổi status của họ dù chứng chỉ cũ có hết hạn hay không). Với mỗi profile:
- Nếu đang `Active` và có Course hết hạn → chuyển `Grounded`, ghi `AuditLog`.
- Nếu đang `Grounded` và không còn Course nào hết hạn → chuyển `Active`, ghi `AuditLog`.

Trả về `GroundedStatusRefreshResponse(ScannedCount, GroundedCount, ClearedCount)` để Admin biết ngay hiệu lực của lần quét.

**Không có background job/cron trong scope này** (đúng với `stack.md`: hệ thống hiện không có hạ tầng job scheduler) — đây là endpoint trigger thủ công. Team vận hành cần tự gọi định kỳ (cron ngoài hệ thống, ví dụ GitHub Actions scheduled workflow hoặc Azure Function Timer Trigger) cho tới khi có hạ tầng job trong app.

### 3.2 `GET /api/etr/due-for-training`

Mở rộng từ `GetExpiringStudentsAsync` đã có sẵn — **không viết lại logic**, chỉ loop qua danh sách `CourseId` (1 course nếu truyền `courseId`, tất cả Course nếu bỏ trống) và gọi lại đúng method cũ cho từng course, gộp kết quả. Điều này giữ nguyên toàn bộ logic phân quyền theo Role đã có (Instructor chỉ thấy lớp mình dạy) mà không cần viết thêm code trùng lặp.

```
GET /api/etr/due-for-training?daysThreshold=30        # tất cả course
GET /api/etr/due-for-training?courseId=5&daysThreshold=90   # 1 course, ngưỡng cảnh báo sớm hơn
```

### 3.3 Tự động clear Grounded — CHỈ khi ETR mới `Completed`, không phải khi Enroll lại

**Quyết định cuối cùng (sau khi xác nhận lại nghiệp vụ):** merely re-enrolling vào lớp mới KHÔNG làm nhân sự "đủ điều kiện bay" trở lại — họ chỉ thực sự đủ điều kiện khi hoàn thành xong khóa đào tạo lại (ETR `Completed`, do Training Manager Approve). Vì vậy:

- `EnrollmentService.CreateEnrollmentAsync` — **không** đụng tới `UserProfile.Status` nữa. Enroll lại chỉ tạo `CourseEnrollment` + `ETRCourseRecord` (`InProgress`) như bình thường; Grounded vẫn giữ nguyên cho tới khi khóa mới hoàn thành.
- `EtrService.CompleteEtrAsync` — sau khi `etr.Status = "Completed"` được lưu (đặt `IssuedDate`/`ExpiryDate` thật), mới kiểm tra và clear Grounded:

```csharp
// Đặt SAU await _unitOfWork.SaveAsync(cancellationToken) của chính việc Complete ETR, để
// CertificateValidityCalculator đọc đúng ExpiryDate/Status vừa được lưu, không phải bản cũ.
var learnerProfile = await _unitOfWork.UserProfileRepository.GetByIdAsync(enrollment.AccountId, cancellationToken);
if (learnerProfile != null && learnerProfile.Status == LearnerStatus.Grounded)
{
    var stillHasExpired = await CertificateValidityCalculator.HasAnyExpiredCompletedEtrAsync(_unitOfWork, enrollment.AccountId, cancellationToken);
    if (!stillHasExpired)
    {
        // ghi AuditLog + set learnerProfile.Status = LearnerStatus.Active + SaveAsync
    }
}
```

`PreviousRecordId` của `ETRCourseRecord` mới đã được set từ logic có sẵn trước đây (`previousEtr?.ETRCourseRecordId`, dựa trên `ETR` gần nhất của cùng Course, thiết lập tại `EnrollmentService.CreateEnrollmentAsync`) — không cần thêm gì cho phần Recurrent Training chaining, chỉ tận dụng lại.

## 4. Rủi ro/giả định cần xác nhận

- ~~**Thời điểm clear Grounded**~~ — **đã xác nhận và sửa** (xem mục 3.3, 7): clear Grounded chỉ xảy ra khi ETR mới `Completed`, không phải khi Enroll lại.
- **Không có background job**: `refresh-grounded-status` chỉ chạy khi được gọi tay hoặc qua cron ngoài hệ thống. Nếu không ai gọi, học viên có chứng chỉ hết hạn sẽ **không tự động** chuyển Grounded (vẫn hiển thị Active) cho tới lần refresh kế tiếp — team vận hành cần thiết lập lịch gọi định kỳ ngay khi go-live tính năng này.
- **`due-for-training` không phân trang**: loop qua toàn bộ Course và gọi lại `GetExpiringStudentsAsync` cho từng course — chấp nhận được ở quy mô dữ liệu hiện tại (nhất quán với pattern `GetAllAsync()` không phân trang đã dùng khắp codebase), nhưng sẽ cần tối ưu nếu số Course tăng lớn.

## 7. Fix cùng ngày: chuyển thời điểm clear Grounded từ "Enroll lại" sang "ETR Completed"

- **Đã xóa** đoạn tự động clear Grounded khỏi `EnrollmentService.CreateEnrollmentAsync` — chỉ còn 1 dòng comment giải thích lý do không làm ở đây, tránh lần sau có người vô tình thêm lại.
- **Đã thêm** đoạn tương đương vào cuối `EtrService.CompleteEtrAsync`, đặt **sau** lệnh `SaveAsync` của chính việc Complete ETR (không phải trước) — lý do: `CertificateValidityCalculator` truy vấn lại `ETRCourseRecordRepository` từ đầu, nếu kiểm tra trước khi save thì sẽ đọc nhầm trạng thái `Verified`/`ExpiryDate=null` cũ của chính ETR đang được Complete, dẫn tới tính sai.
- **Test đã cập nhật**: `EnrollmentServiceTests` đổi tên/assertion để xác nhận Enroll lại **không** tự clear Grounded (`CreateEnrollmentAsync_GroundedLearnerReenrolls_RemainsGroundedUntilNewEtrIsCompleted`); thêm 2 test mới trong `EtrServiceTests` cho `CompleteEtrAsync` (clear khi hết hạn duy nhất đã được thay bằng record mới hoàn thành; vẫn Grounded nếu còn 1 course khác đang hết hạn).

## 5. Đã kiểm chứng bằng cách nào

- **TDD**: viết test trước cho từng nhánh hành vi (Grounded mới, clear Grounded, Withdrawn không bị đụng, due-for-training quét toàn bộ course, Enroll lại clear/giữ Grounded tùy còn course khác hết hạn hay không) → xác nhận RED (build lỗi vì method chưa tồn tại trên `IEtrService`) → implement → xác nhận GREEN.
- `dotnet build` toàn solution: **0 Error** (2 warning pre-existing, không liên quan).
- `dotnet test`: **34/34 pass** (27 cũ + 7 mới: `EtrServiceTests` 6 case gồm 2 case mới cho `CompleteEtrAsync`, `EnrollmentServiceTests` 1 case), không regression.
- Test case đáng chú ý: `CreateEnrollmentAsync_GroundedLearnerReenrolls_RemainsGroundedUntilNewEtrIsCompleted` xác nhận Enroll lại KHÔNG tự clear Grounded; `CompleteEtrAsync_GroundedLearnerWithNoOtherExpiredCourse_ClearsBackToActive` xác nhận clear đúng lúc `Completed`; `CompleteEtrAsync_GroundedLearnerHasAnotherStillExpiredCourse_RemainsGrounded` xác nhận KHÔNG clear nhầm khi học viên còn 1 course khác đang hết hạn.

## 6. Files liên quan

| File | Trạng thái |
|---|---|
| `ETR.Application/Compliance/LearnerStatus.cs` | Mới |
| `ETR.Application/Compliance/CertificateValidityCalculator.cs` | Mới |
| `ETR.Application/DTOs/Etr/Responses/GroundedStatusRefreshResponse.cs` | Mới |
| `ETR.Application/Interfaces/IEtrService.cs` | Sửa — thêm `GetDueForTrainingAsync`, `RefreshGroundedStatusAsync` |
| `ETR.Application/Services/EtrService.cs` | Sửa — implement 2 method mới |
| `ETR.Application/Services/EnrollmentService.cs` | Sửa — KHÔNG còn tự clear Grounded ở đây (fix cùng ngày, xem mục 7) |
| `ETR.API/Controllers/EtrController.cs` | Sửa — thêm `GET due-for-training`, `POST refresh-grounded-status` |
| `ETR.Application.Tests/Services/EtrServiceTests.cs` | Mới — 6 test case (gồm 2 case `CompleteEtrAsync` thêm ở fix cùng ngày) |
| `ETR.Application.Tests/Services/EnrollmentServiceTests.cs` | Mới — 1 test case (đổi từ 2 case ban đầu sau fix cùng ngày) |
| `docs/todo/9.todo_to_complete_system.md`, `ETR.Documentation/final/9.todo_to_complete_system.md` | Sửa — đánh dấu mục #4 đã triển khai |
