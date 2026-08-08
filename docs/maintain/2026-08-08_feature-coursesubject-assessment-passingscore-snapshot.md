# Feature: Versioning PassingScore cho CourseSubject & Assessment (Snapshot Model) — 2026-08-08

**Ngày thực hiện:** 2026-08-08
**Phạm vi:** `ETR.Domain/Entities/{SubjectResult,AssessmentResult}.cs` (mở rộng); `ETR.Infrastructure/Migrations/*_AddPassingScoreSnapshot.cs`, `*_AddWeightSnapshot.cs` (mới); `ETR.Infrastructure/Data/AppDbContext.cs` (mở rộng); `ETR.Application/Services/{EnrollmentService,AssessmentResultService,PracticalChecklistResultService}.cs` (mở rộng); test mới `ETR.Application.Tests/Services/AssessmentResultServiceTests.cs`.
**Mục tiêu:** `/mpower:code-fix` — xử lý điểm còn tồn đọng thật của mục #3 trong `docs/todo/9.todo_to_complete_system.md` (Versioning cho Course & CompletionRequirement): "`CourseSubject`/`Assessment` (VD: `PassingScore`) chưa được version hóa — nếu phát hiện rủi ro tương tự cần một lượt versioning riêng."

> **Cập nhật cùng ngày (2026-08-08):** đã bổ sung snapshot cho `Assessment.Weight` (dùng tính điểm trung bình có trọng số) — điểm còn tồn đọng duy nhất mục 4 tự nêu ra ban đầu. Xem mục 3.5 và 7.

---

## 1. Vấn đề trước khi sửa

Rà soát xác nhận rủi ro **thật, không phải suy đoán**: `AssessmentResultService.RecordAssessmentScoreAsync` (4 vị trí) và `EvaluateSubjectPassabilityAsync` đều đọc **live** `Assessment.PassingScore`/`CourseSubject.PassingScore` mỗi lần chấm điểm hoặc đánh giá lại — hoàn toàn giống lỗ hổng đã sửa cho `CompletionRequirement` trước đó (nếu Admin đổi điểm đạt giữa khóa, học viên được chấm ở các thời điểm khác nhau bị áp 2 luật khác nhau; nghiêm trọng hơn, nếu Instructor re-sign-off sau khi Amendment mở lại Subject, `EvaluateSubjectPassabilityAsync` chấm lại theo ngưỡng **hiện tại**, có thể lật ngược kết quả một cách âm thầm).

## 2. Vì sao KHÔNG dùng đúng pattern append-only-row như CompletionRequirement

Đã thử áp dụng y hệt pattern "đóng row cũ, tạo row mới với VersionNo tăng" — phát hiện **CourseSubject dùng composite Primary Key `(CourseId, SubjectId)`, không có surrogate ID riêng** (khác `CompletionRequirement.RequirementId`). Không thể có 2 row cùng `(CourseId, SubjectId)` dưới PK hiện tại, nên không thể "thêm 1 version mới" mà giữ nguyên PK — muốn làm đúng kiểu này phải đổi cấu trúc khóa chính (VD: thêm `VersionNo` vào composite key), một thay đổi lớn hơn nhiều, ảnh hưởng mọi nơi đang query theo `(CourseId, SubjectId)` và cần quyết định thiết kế riêng, không tự ý làm trong lượt sửa nhỏ này.

**Giải pháp thay thế — Snapshot tại thời điểm phát sinh dữ liệu**: thay vì versioning bảng gốc, snapshot giá trị `PassingScore` đang áp dụng **ngay vào chính bản ghi kết quả** (`SubjectResult`, `AssessmentResult`) tại thời điểm nó được tạo ra — cùng triết lý với `ETRCourseRecord.CourseVersionNo` (snapshot tại Enroll), chỉ khác là snapshot thẳng giá trị `decimal` thay vì một con số version cần tra ngược lại bảng gốc. Về hiệu lực chống hồi tố, kết quả tương đương; về xâm lấn schema, nhẹ hơn nhiều (chỉ thêm 1 cột nullable, không đổi khóa chính, không phải sinh thêm row).

## 3. Chi tiết implementation

### 3.1 Entity mới
- `SubjectResult.PassingScoreSnapshot` (`decimal?`) — snapshot `CourseSubject.PassingScore` tại Enroll.
- `AssessmentResult.PassingScoreSnapshot` (`decimal?`) — snapshot `Assessment.PassingScore` tại lần chấm điểm ĐẦU TIÊN của learner cho assessment đó.

Cả 2 đều **nullable** — bản ghi tạo trước migration này có `null`, tự động fallback về giá trị live (không breaking dữ liệu cũ).

### 3.2 Điểm snapshot — `EnrollmentService.CreateEnrollmentAsync`
Khi tạo `SubjectResult`/`AssessmentResult` placeholder lúc Enroll, snapshot luôn `cs.PassingScore`/`assessment.PassingScore` hiện tại vào record mới — đúng giá trị "luật đang áp dụng khi học viên bắt đầu khóa này".

### 3.3 Áp dụng snapshot khi chấm điểm — `AssessmentResultService`
- `RecordAssessmentScoreAsync`: cả 3 nhánh (điền vào placeholder có sẵn, sửa điểm chưa publish, tạo attempt mới/retake) đều ưu tiên `PassingScoreSnapshot` đã có trên record liên quan (placeholder hoặc `latestResult`) thay vì đọc lại `assessment.PassingScore`. **Toàn bộ chuỗi attempt của cùng 1 assessment (lần 1, thi lại lần 2, 3...) luôn dùng chung 1 snapshot** — không để mỗi lần thi lại bị chấm theo 1 ngưỡng khác nhau. Nếu KHÔNG có bản ghi nào trước đó (assessment được thêm vào course SAU khi học viên đã enroll — không có "luật cũ" nào để giữ), mới fallback đọc live — đúng ý nghĩa vì trường hợp này không có gì để bảo toàn.
- `UpdateAssessmentResultAsync`: dùng `result.PassingScoreSnapshot ?? assessment?.PassingScore`.
- `EvaluateSubjectPassabilityAsync` (hàm chấm Pass/Fail cấp Subject, chạy lại mỗi lần Sign-off — kể cả sign-off lại sau khi Amendment mở khóa): dùng `subjectResult.PassingScoreSnapshot ?? courseSubject?.PassingScore ?? 50m`, đồng thời backfill snapshot nếu bản ghi cũ chưa có (`??=`).

### 3.4 `PracticalChecklistResultService.GetPassingScoreAsync`
Đổi signature nhận thẳng `SubjectResult` thay vì `(courseId, subjectId)` rời rạc, ưu tiên đọc `subjectResult.PassingScoreSnapshot` trước khi truy vấn `CourseSubject` live — cùng nguyên tắc áp dụng cho Practical Checklist.

### 3.5 `AssessmentResult.WeightSnapshot` — fix bổ sung cùng ngày

`CalculateSubjectResultScoreAsync` (tính điểm trung bình có trọng số của `SubjectResult` từ toàn bộ `AssessmentResult` con) đọc **live** `Assessment.Weight` mỗi lần được gọi lại (sau MỌI lần ghi điểm mới, kể cả cho 1 assessment khác trong cùng Subject) — cùng họ rủi ro với `PassingScore`: nếu Admin đổi `Weight` giữa khóa, điểm tổng hợp của TẤT CẢ học viên đã có điểm trước đó bị tính lại sai lệch ngay ở lần ghi điểm tiếp theo (không cần ai chạm vào assessment đã đổi Weight).

Áp dụng đúng pattern snapshot đã dùng cho `PassingScore`:
- `AssessmentResult.WeightSnapshot` (`decimal?`, nullable) — snapshot `Assessment.Weight` tại cùng thời điểm với `PassingScoreSnapshot` (Enroll time, hoặc lần chấm điểm đầu tiên nếu assessment được thêm sau).
- `EnrollmentService.CreateEnrollmentAsync`: set `WeightSnapshot = assessment.Weight` song song với `PassingScoreSnapshot`.
- `AssessmentResultService.RecordAssessmentScoreAsync`: cả 3 nhánh dùng `weight = <record hiện có>.WeightSnapshot ?? assessment.Weight`, backfill `??=` giống `PassingScoreSnapshot`.
- `CalculateSubjectResultScoreAsync`: `var weight = result.WeightSnapshot ?? assessment?.Weight;` thay cho `assessment.Weight` trực tiếp — mỗi `AssessmentResult` đóng góp vào điểm trung bình đúng theo trọng số **tại thời điểm nó được ghi**, không phải trọng số hiện tại của Assessment.

## 4. Việc CHƯA làm / rủi ro đã biết

- **`CourseSubject`/`Assessment` vẫn không có lịch sử row-level như `CompletionRequirement`** — không ai truy được "PassingScore/Weight của assessment này TỪNG là bao nhiêu vào ngày X" trừ khi tra `AuditLog` (nếu có ghi) hoặc suy ra gián tiếp từ các `AssessmentResult.PassingScoreSnapshot`/`WeightSnapshot` đã lưu. Đây là đánh đổi có chủ đích đã giải thích ở mục 2 — full versioning cần đổi composite PK, để dành cho 1 quyết định thiết kế riêng nếu team thực sự cần truy vết lịch sử đầy đủ cấp bảng gốc.
- Chưa đồng bộ cột mới vào 3 file raw SQL deploy thủ công (`Deploy_ETR_System.sql`, `ALL_IN_ONE_Deploy.sql`, `Deploy_NukeAndSeed.sql`) — cùng tình trạng lạc hậu ~10 migration đã ghi nhận ở các lượt sửa trước; khuyến nghị team dùng `dotnet ef migrations script --idempotent` để sinh lại toàn bộ thay vì vá tay.

## 5. Đã kiểm chứng bằng cách nào

- **TDD**: viết test trước cho từng nhánh hành vi cốt lõi (placeholder đã có snapshot cũ → chấm theo snapshot dù PassingScore hiện tại đã đổi; không có bản ghi nào trước đó → fallback đúng live value và tự lưu snapshot cho lần sau; `WeightSnapshot` được giữ nguyên qua lần ghi điểm mới, không bị ghi đè bởi `Assessment.Weight` hiện tại) → xác nhận RED (build lỗi vì field chưa tồn tại) → implement → xác nhận GREEN.
- `dotnet build` toàn solution: **0 Error** (2 warning pre-existing, không liên quan).
- `dotnet ef migrations add AddPassingScoreSnapshot` / `AddWeightSnapshot`: lần đầu sinh ra cảnh báo thiếu cấu hình precision cho cột decimal mới → đã sửa `AppDbContext.ConfigureDecimalPrecision` (thêm `HasColumnType("decimal(5,2)")`, đúng theo convention đã dùng cho mọi cột decimal khác trong hệ thống) → xóa migration `PassingScoreSnapshot` cũ, sinh lại — không còn cảnh báo; `WeightSnapshot` sinh đúng ngay từ đầu vì đã thêm cấu hình trước.
- `dotnet test`: **59/59 pass** (56 cũ + 3 mới), không regression.

## 6. Files liên quan

| File | Trạng thái |
|---|---|
| `ETR.Domain/Entities/SubjectResult.cs` | Sửa — thêm `PassingScoreSnapshot` |
| `ETR.Domain/Entities/AssessmentResult.cs` | Sửa — thêm `PassingScoreSnapshot`, `WeightSnapshot` |
| `ETR.Infrastructure/Migrations/*_AddPassingScoreSnapshot.cs`, `*_AddWeightSnapshot.cs` | Mới |
| `ETR.Infrastructure/Data/AppDbContext.cs` | Sửa — thêm `HasColumnType("decimal(5,2)")` cho 3 cột mới |
| `ETR.Application/Services/EnrollmentService.cs` | Sửa — snapshot cả `PassingScore` và `Weight` tại Enroll |
| `ETR.Application/Services/AssessmentResultService.cs` | Sửa — dùng snapshot thay vì live PassingScore/Weight ở `RecordAssessmentScoreAsync`, `UpdateAssessmentResultAsync`, `EvaluateSubjectPassabilityAsync`, `CalculateSubjectResultScoreAsync` |
| `ETR.Application/Services/PracticalChecklistResultService.cs` | Sửa — `GetPassingScoreAsync` ưu tiên snapshot |
| `ETR.Application.Tests/Services/AssessmentResultServiceTests.cs` | Mới — 3 test case |
| `docs/todo/9.todo_to_complete_system.md`, `ETR.Documentation/final/9.todo_to_complete_system.md` | Sửa — cập nhật mục #3 |
