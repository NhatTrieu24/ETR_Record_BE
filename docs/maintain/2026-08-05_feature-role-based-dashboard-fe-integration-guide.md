# Feature: Role-based Dashboard (`GET /api/dashboard/my-dashboard`) — Hướng dẫn triển khai Frontend — 2026-08-05

**Ngày thực hiện:** 2026-08-05
**Phạm vi:** `ETR.API/Controllers/DashboardController.cs`; `ETR.Application/Services/{DashboardService,DashboardKpiCalculator}.cs` (mới + mở rộng); `ETR.Application/Interfaces/IDashboardService.cs` (mới); `ETR.Application/DTOs/Dashboard/MyDashboardResponse.cs` (mới); `ETR.Application/DependencyInjection.cs`; test mới `ETR.Application.Tests/Services/DashboardServiceTests.cs`.
**Mục tiêu:** `/mpower:code-new-feature` (TDD-first) — xây 1 endpoint Dashboard duy nhất, tự động trả đúng widget theo vai trò người gọi, để FE có 1 nơi duy nhất gọi khi load trang Dashboard thay vì phải tự biết role nào cần gọi API nào.

---

## 1. Tại sao lại là 1 endpoint duy nhất, không phải nhiều endpoint theo role?

Hệ thống có 8 role (Admin, Instructor, QA, Academic, TrainingManager, Student, Audit, ManagementViewer) với nhu cầu Dashboard rất khác nhau. Thay vì bắt FE tự if/else theo role rồi gọi đúng API, `GET /api/dashboard/my-dashboard` tự nhận diện role của người gọi (qua JWT) và trả về **đúng một shape response duy nhất**, trong đó mỗi field chỉ được điền (khác `null`) nếu role đó cần dùng đến. FE chỉ cần:

```
GET /api/dashboard/my-dashboard
Authorization: Bearer <token>
```

rồi check field nào khác `null` để biết hiển thị widget nào — không cần biết trước "role X gọi API Y".

## 2. Response shape đầy đủ (FE dùng chung 1 TypeScript interface)

```ts
interface MyDashboardResponse {
  role: string;                 // "Admin" | "Instructor" | "QA" | "Academic" | "TrainingManager" | "Student" | "Audit" | "ManagementViewer"
  generatedAt: string;          // ISO datetime, giờ server tạo response — dùng để hiển thị "cập nhật lúc..."
  overview: DashboardKpis | null;
  statusFunnel: DashboardStatusFunnel | null;
  actionItems: DashboardActionItems | null;
  myClasses: InstructorClassSummary[] | null;
  lowAttendanceStudents: LowAttendanceStudentResponse[] | null;
  pendingVerificationEtrIds: number[] | null;
  myEtrs: StudentEtrSummary[] | null;
}

interface DashboardKpis {
  totalEtrs: number;
  completedCount: number;
  completionRatePercent: number;
  pendingApprovalCount: number;
  rejectedCount: number;
  returnedForCorrectionCount: number;
  missingEvidenceCount: number;
}

interface DashboardStatusFunnel {
  draft: number;
  inProgress: number;
  submitted: number;
  verified: number;
  completed: number;
  returnedForCorrection: number;
  cancelled: number;
}

interface DashboardActionItems {
  pendingApprovalEtrIds: number[];
  rejectedEtrIds: number[];
  returnedForCorrectionEtrIds: number[];
  missingEvidenceEtrIds: number[];
}

interface InstructorClassSummary {
  classId: number;
  classCode: string;
  className: string;
  studentCount: number;
}

interface LowAttendanceStudentResponse {
  accountId: number;
  userCode: string;
  fullName: string;
  classId: number;
  classCode: string;
  subjectId: number;
  subjectCode: string;
  attendanceRate: number;
  thresholdPercent: number;   // 80 — ngưỡng tối thiểu, dùng để hiển thị "X% (dưới ngưỡng Y%)"
}

interface StudentEtrSummary {
  etrCourseRecordId: number;
  status: string;
  percentComplete: number;    // 0-100
  expiryDate: string | null;
}
```

## 3. Field nào được điền theo từng role — bảng tra cứu cho FE

| Role | `overview` | `statusFunnel` | `actionItems` | `myClasses` | `lowAttendanceStudents` | `pendingVerificationEtrIds` | `myEtrs` |
|---|:-:|:-:|:-:|:-:|:-:|:-:|:-:|
| Admin | ✅ | ✅ | ✅ | — | — | — | — |
| TrainingManager | ✅ | ✅ | ✅ | — | — | — | — |
| Academic | ✅ | ✅ | ✅ | — | — | — | — |
| Audit | ✅ | ✅ | ✅ | — | — | — | — |
| ManagementViewer | ✅ | ✅ | ✅ | — | — | — | — |
| Instructor | — | — | — | ✅ | ✅ (chỉ lớp mình dạy) | — | — |
| QA | — | — | ✅ | — | — | ✅ | — |
| Student | — | — | — | — | — | — | ✅ (chỉ ETR của chính mình) |

**Gợi ý UI theo role (widget đề xuất, không bắt buộc theo đúng layout):**

- **Admin/TrainingManager/Academic/Audit/ManagementViewer**: 4 KPI card từ `overview` (Total ETRs, Completion Rate %, Pending Approval, Missing Evidence) → biểu đồ funnel từ `statusFunnel` (cột ngang: Draft→InProgress→Submitted→Verified→Completed, có thêm 2 cột riêng ReturnedForCorrection/Cancelled) → 4 danh sách click-through từ `actionItems` (mỗi ID click vào mở chi tiết ETR tương ứng qua `GET /api/etr/{id}`).
- **Instructor**: danh sách lớp (`myClasses`, mỗi dòng có nút "Xem chi tiết" → `GET /api/enrollments?classId=`) + bảng cảnh báo điểm danh thấp (`lowAttendanceStudents`, hiển thị dạng badge đỏ nếu `attendanceRate < thresholdPercent`).
- **QA**: danh sách ETR chờ Verify (`pendingVerificationEtrIds`, mỗi ID có nút "Verify" gọi thẳng `POST /api/etr/{id}/verify`) + `actionItems.missingEvidenceEtrIds` để biết ETR nào sắp bị chặn Verify do thiếu evidence.
- **Student**: mỗi ETR trong `myEtrs` hiển thị progress bar theo `percentComplete`, kèm badge trạng thái (`status`) và ngày hết hạn (`expiryDate`, nếu có). Click vào 1 ETR → gọi thêm `GET /api/etr/{id}/completion-progress` để xem chi tiết từng điều kiện còn thiếu (mục 4 bên dưới).

## 4. Các endpoint hỗ trợ (drill-down) — FE sẽ cần gọi thêm khi người dùng click vào 1 item

Các endpoint này ĐÃ CÓ SẴN từ batch trước (2026-08-05, xem `docs/maintain/2026-08-05_hoan-thien-medium-priority-m1-m14.md`), không phải mới trong lượt này — liệt kê lại ở đây để FE có đủ thông tin triển khai trọn vẹn luồng Dashboard mà không phải lục lại tài liệu cũ:

| Endpoint | Dùng khi nào | Role được gọi |
|---|---|---|
| `GET /api/dashboard/stats` | Vẫn giữ nguyên, số liệu tổng quan dạng cũ (nếu màn hình nào đó chỉ cần đúng 1 KPI card, không cần cả bộ `my-dashboard`) | Admin, TrainingManager, Audit, Academic, ManagementViewer |
| `GET /api/dashboard/action-items` | Tương tự — nếu chỉ cần riêng phần action items | Admin, TrainingManager, Audit, Academic, ManagementViewer |
| `GET /api/etr/{id}/completion-progress` | Click vào 1 ETR trong `myEtrs` để xem chi tiết từng điều kiện Submit còn thiếu | Instructor, QA, Admin, Audit, Academic, TrainingManager |
| `GET /api/attendance/low-attendance?classId=` | Instructor lọc riêng theo 1 lớp cụ thể thay vì xem tất cả lớp mình dạy cùng lúc | Instructor, Admin, Academic, Audit, QA |
| `GET /api/etr/expiring-students?courseId=&daysThreshold=` | Widget "học viên sắp hết hạn chứng chỉ" (chưa gộp vào `my-dashboard`, gọi riêng nếu cần) | Admin, Instructor, QA, TrainingManager, Academic |

## 5. Xác thực

`GET /api/dashboard/my-dashboard` chỉ yêu cầu đăng nhập hợp lệ (`[Authorize]` không giới hạn role) — bất kỳ role nào gọi cũng nhận `200`, tự động lọc theo danh tính trong JWT. Riêng `stats`/`action-items` (endpoint cũ) **vẫn giữ nguyên giới hạn 5 role như trước** (Admin, TrainingManager, Audit, Academic, ManagementViewer) — không đổi hành vi cũ.

## 6. Đã kiểm chứng bằng cách nào

- **TDD**: viết test trước (`DashboardServiceTests.cs`, 4 test case — Admin/Instructor/QA/Student) → xác nhận RED (build lỗi vì `DashboardService` chưa tồn tại) → implement → xác nhận GREEN.
- `dotnet build` toàn solution: **0 Error**.
- `dotnet test`: **27/27 pass** (23 cũ + 4 mới), không regression.
- Chạy app thật (Docker SQL Server dev), gọi `GET /api/dashboard/my-dashboard` với cả 4 nhóm role (Admin, Instructor, QA, Student) — xác nhận đúng field nào được điền theo từng role, field khác đều `null`.
- Xác nhận không phá vỡ hành vi cũ: `GET /api/dashboard/stats`/`action-items` vẫn `200` với Admin, vẫn `403` với Instructor (đúng như giới hạn role cũ).

## 7. Rủi ro/việc còn lại

- `myClasses`/`lowAttendanceStudents` (Instructor) hiện load TOÀN BỘ lớp Instructor phụ trách trong 1 lần gọi — nếu 1 Instructor dạy rất nhiều lớp, có thể cần thêm phân trang ở phiên bản sau (chưa cần thiết ở quy mô dữ liệu hiện tại).
- `myEtrs` (Student) gọi `GetCompletionProgressAsync` cho từng ETR — với Student thường chỉ có 1-2 ETR nên không phải vấn đề hiệu năng ở quy mô hiện tại; theo dõi nếu 1 học viên có lịch sử ETR nhiều lần (`PreviousRecordId` chain dài).
- Chưa có test cho case role không nằm trong 8 role đã biết (role lạ/null) — hành vi hiện tại: tất cả field trả `null` trừ `role`/`generatedAt`, không lỗi — đây là fallback an toàn nhưng chưa có test tường minh xác nhận.

## 8. Files liên quan

| File | Trạng thái |
|---|---|
| `ETR.Application/DTOs/Dashboard/MyDashboardResponse.cs` | Mới |
| `ETR.Application/Interfaces/IDashboardService.cs` | Mới |
| `ETR.Application/Services/DashboardService.cs` | Mới |
| `ETR.Application/Services/DashboardKpiCalculator.cs` | Sửa — thêm `ComputeStatusFunnelAsync` |
| `ETR.Application/DependencyInjection.cs` | Sửa — đăng ký `IDashboardService` |
| `ETR.API/Controllers/DashboardController.cs` | Sửa — demote class-level `[Authorize]` (bỏ role-list chung, mỗi action tự khai báo role riêng theo đúng pattern đã dùng ở `EtrController`), thêm endpoint `GET my-dashboard` |
| `ETR.Application.Tests/Services/DashboardServiceTests.cs` | Mới — 4 test case (Admin/Instructor/QA/Student) |
