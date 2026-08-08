# Feature: Data Isolation cho Instructor — "Sân nhà ai nấy đá" — 2026-08-08

**Ngày thực hiện:** 2026-08-08
**Phạm vi:** `ETR.Application/Compliance/ClassOwnershipValidator.cs` (mới); `ETR.Application/Services/{ClassService,AttendanceService,AssessmentResultService,EvidenceService}.cs` (mở rộng); `ETR.Application/Interfaces/{IClassService,IAttendanceService,IAssessmentResultService,IEvidenceService}.cs` (mở rộng — thực ra `IClassService` không đổi signature, chỉ 3 interface còn lại đổi); `ETR.API/Controllers/{AttendanceController,AssessmentResultsController,EvidencesController}.cs` (mở rộng); test mới `ETR.Application.Tests/Services/{ClassServiceTests,AttendanceServiceTests}.cs`, mở rộng `AssessmentResultServiceTests.cs`, `EvidenceServiceTests.cs`.
**Mục tiêu:** `/mpower:code-fix` — triển khai mục #11 trong `docs/todo/9.todo_to_complete_system.md` (Data Isolation cho Instructor), nguyên tắc "Cô lập dữ liệu theo phân quyền" team thống nhất trong `docs/todo/addition.md`. Đây là gap được đánh giá **nghiêm trọng nhất** trong toàn bộ backlog vì ảnh hưởng trực tiếp đến tính xác thực của MỌI dữ liệu đào tạo, không riêng 1 tính năng.

---

## 1. Vấn đề trước khi sửa

Rà soát xác nhận **lỗ hổng bảo mật thật, không phải suy đoán**: cơ chế bảo vệ duy nhất cho 5 điểm chạm dưới đây trước đây chỉ là `[Authorize(Roles = "Instructor,Admin")]` cấp controller — kiểm tra VAI TRÒ, không kiểm tra DANH TÍNH/QUYỀN SỞ HỮU. Bất kỳ Instructor nào (dù không được phân công) cũng gọi được các API này cho BẤT KỲ lớp/session/subject nào nếu biết ID:

1. `GET /api/classes` — `ClassService.GetAllClassesAsync` trả về TOÀN BỘ lớp trong hệ thống.
2. `POST /api/attendance/record` — `AttendanceService.RecordAttendanceAsync`.
3. `POST /api/assessmentresults/record` — `AssessmentResultService.RecordAssessmentScoreAsync`.
4. `POST /api/subjectsignoff` — `AssessmentResultService.SignoffSubjectResultAsync`.
5. `POST /api/evidences/upload` — `EvidenceService.UploadEvidenceAsync`.

## 2. Thiết kế

### 2.1 `ClassOwnershipValidator` — helper dùng chung cho cả 4 điểm ghi dữ liệu

```csharp
public static class ClassOwnershipValidator
{
    public static void EnsureInstructorOwnsClass(string? callerRoleName, int? callerAccountId, int? classInstructorAccountId)
    {
        if (!string.Equals(callerRoleName, "Instructor", StringComparison.OrdinalIgnoreCase)) return;
        if (classInstructorAccountId == null || classInstructorAccountId != callerAccountId)
            throw new ForbiddenAccessException("Bạn không được phân công giảng dạy lớp này.");
    }
}
```

Chỉ áp dụng cho role `Instructor` — Admin/Academic/TrainingManager/QA/Audit **không bị giới hạn** bởi check này (đúng theo yêu cầu gốc — `[Authorize(Roles=...)]` ở controller đã quyết định role nào được gọi hành động, validator này chỉ bổ sung lớp kiểm tra danh tính RIÊNG cho Instructor).

### 2.2 Vì sao truyền `roleName` tường minh thay vì tự lấy từ `ICurrentUserService` bên trong Service

3 trong 4 service (`AttendanceService`, `AssessmentResultService`, `EvidenceService`) vốn không inject `ICurrentUserService` — chúng nhận danh tính người gọi qua tham số tường minh (`recordedByAccountId`, ...), đúng pattern đã có sẵn của `AssessmentResultService.SignoffSubjectResultAsync` (vốn đã nhận `signoffByRoleName` từ trước). Theo cùng convention đó, cả 3 method còn lại (`RecordAttendanceAsync`, `RecordAssessmentScoreAsync`, `UploadEvidenceAsync`) được thêm tham số `string? ...RoleName`, do Controller truyền vào từ `_currentUserService.RoleName` — giữ Service tầng thuần túy (dễ test, không phụ thuộc ngầm vào request context).

Riêng `ClassService` **có** inject `ICurrentUserService` trực tiếp vì `GetAllClassesAsync` không nhận tham số actor nào từ trước (không có ai truyền accountId vào) — theo đúng pattern đã dùng ở `EtrService`/`DashboardService`.

### 2.3 Cách mỗi điểm ghi dữ liệu resolve ra "Class của hành động này"

| Method | Đường resolve ra `Class` |
|---|---|
| `AttendanceService.RecordAttendanceAsync` | `Session.ClassId` → `Class` |
| `AssessmentResultService.RecordAssessmentScoreAsync` | Tìm `Class` mà learner (`request.AccountId`) đang enroll VÀ có `CourseId` khớp `Assessment.CourseId` — tận dụng lại đúng logic `isEnrolledInAssessmentCourse` đã có sẵn, chỉ đổi từ `.Any(...)` thành `.FirstOrDefault(...)` để lấy được object `Class` thay vì chỉ `bool` |
| `AssessmentResultService.SignoffSubjectResultAsync` | `SubjectResult.EtrId` → `ETRCourseRecord.EnrollmentId` → `CourseEnrollment.ClassId` → `Class` |
| `EvidenceService.UploadEvidenceAsync` | `SubjectResult.EtrId` → `ETRCourseRecord.EnrollmentId` → `CourseEnrollment.ClassId` → `Class` (check chạy TRƯỚC khi ghi file vào đĩa, tránh để lại file rác khi request bị từ chối) |

## 3. Ngoại lệ dạy thay/chấm chéo — ĐÃ CHỐT HOÃN LẠI

Team đã quyết định trong `docs/todo/addition.md`: case "1 Class có nhiều Instructor được cấp quyền" (dạy thay, chấm thi chéo) là tính năng phụ, **không làm trong lượt này**. `Class.InstructorAccountId` giữ nguyên dạng single-value; **không** tự ý tạo bảng trung gian (`ClassInstructors_Mapping`) — tránh over-engineering cho use-case chưa ai yêu cầu cụ thể. Nếu sau này cần, đây sẽ là điểm mở rộng tự nhiên: thay `classInstructorAccountId` (1 giá trị) bằng danh sách "AccountId nào được cấp quyền cho Class X", và `ClassOwnershipValidator` chỉ cần đổi từ so sánh bằng sang kiểm tra tồn tại trong danh sách.

## 4. Việc CHƯA làm / rủi ro đã biết

- **`SessionsController` (tạo/sửa/xóa Session), `SubjectSignoffController.RequestUnlock`** chưa được rà soát cho cùng loại check — todo doc gốc chỉ liệt kê đích danh 4 method + `GetAllClassesAsync`; các endpoint khác thao tác gián tiếp lên Class/Session của Instructor (VD: `POST /api/sessions`, xác nhận session) nằm ngoài phạm vi yêu cầu lần này, nên rà soát thêm nếu phát hiện rủi ro tương tự.
- **`GET /api/classstudents`, `GET /api/attendance` (danh sách), các API "xem" khác** chưa được audit toàn diện theo nguyên tắc "sân nhà ai nấy đá" — chỉ `GET /api/classes` (đích danh trong todo doc) được xử lý trong lượt này.
- Ngoại lệ dạy thay/chấm chéo — xem mục 3, đã chốt hoãn có chủ đích, không phải thiếu sót.

## 5. Đã kiểm chứng bằng cách nào

- **TDD**: viết test trước cho từng điểm chạm (Instructor chỉ thấy lớp được phân công khi gọi danh sách lớp; Admin thấy toàn bộ; Instructor không được phân công bị chặn 403 khi điểm danh/chấm điểm/ký xác nhận/upload minh chứng; Instructor được phân công đúng vẫn thao tác bình thường) → xác nhận RED (build lỗi vì đổi signature các method) → implement → xác nhận GREEN.
- `dotnet build` toàn solution: **0 Error** (2 warning pre-existing, không liên quan).
- `dotnet test`: **66/66 pass** (59 cũ + 7 mới: `ClassServiceTests` 2 case, `AttendanceServiceTests` 2 case, `AssessmentResultServiceTests` +2 case, `EvidenceServiceTests` +1 case), không regression — các test cũ đã cập nhật fixture (`InstructorAccountId` khớp `recordedByAccountId`/`signoffByAccountId` dùng trong test) để phản ánh đúng hành vi thật của 1 Instructor thao tác trên lớp của chính mình.

## 6. Files liên quan

| File | Trạng thái |
|---|---|
| `ETR.Application/Compliance/ClassOwnershipValidator.cs` | Mới |
| `ETR.Application/Services/ClassService.cs` | Sửa — inject `ICurrentUserService`, filter `GetAllClassesAsync` theo Instructor |
| `ETR.Application/Services/AttendanceService.cs` | Sửa — thêm tham số `recordedByRoleName`, check ownership qua `Session.ClassId` |
| `ETR.Application/Services/AssessmentResultService.cs` | Sửa — thêm tham số role cho `RecordAssessmentScoreAsync`, check ownership ở cả `RecordAssessmentScoreAsync` và `SignoffSubjectResultAsync` |
| `ETR.Application/Services/EvidenceService.cs` | Sửa — thêm tham số `uploadedByRoleName`, check ownership TRƯỚC khi ghi file |
| `ETR.Application/Interfaces/{IAttendanceService,IAssessmentResultService,IEvidenceService}.cs` | Sửa — thêm tham số role vào signature |
| `ETR.API/Controllers/{AttendanceController,AssessmentResultsController,EvidencesController}.cs` | Sửa — truyền `_currentUserService.RoleName` vào service |
| `ETR.Application.Tests/Services/ClassServiceTests.cs` | Mới — 2 test case |
| `ETR.Application.Tests/Services/AttendanceServiceTests.cs` | Mới — 2 test case |
| `ETR.Application.Tests/Services/AssessmentResultServiceTests.cs` | Sửa — thêm 2 test case, cập nhật fixture cũ |
| `ETR.Application.Tests/Services/EvidenceServiceTests.cs` | Sửa — thêm 1 test case |
| `docs/todo/9.todo_to_complete_system.md`, `ETR.Documentation/final/9.todo_to_complete_system.md` | Sửa — đánh dấu mục #11 đã triển khai |
