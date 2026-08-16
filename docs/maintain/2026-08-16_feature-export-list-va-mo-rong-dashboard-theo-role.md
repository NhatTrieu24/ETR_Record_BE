# Feature: Export Jobs list (`GET /api/exports`) + Mở rộng `GET /api/dashboard/my-dashboard` theo role — 2026-08-16

**Ngày thực hiện:** 2026-08-16
**Phạm vi:** `ETR.API/Controllers/ExportsController.cs` (sửa — thêm 1 action); `ETR.Application/Services/{DashboardService,DashboardKpiCalculator}.cs` (sửa — mở rộng); `ETR.Application/DTOs/Dashboard/MyDashboardResponse.cs` (sửa — thêm DTO mới); test mới `ETR.Application.Tests/Services/DashboardKpiCalculatorTests.cs`.
**Mục tiêu:** `/mpower:code-new-feature` — (1) thay số "Audit Packages Exported" đang bị tính ảo bằng 1 API list thật cho `ExportJob`; (2) mở rộng `my-dashboard` để mỗi role chỉ cần đúng 1 lần gọi `fetchMyDashboard()` là có đủ dữ liệu cho màn hình Dashboard của mình, không phải tự gọi thêm nhiều API rời rạc.

---

## 1. API mới — `GET /api/exports?page=1&pageSize=10`

`ExportsController` đã có sẵn hạ tầng `ExportJob` đầy đủ (entity, repository, `ExportJobResponse`) từ tính năng export trước đó, nhưng chưa có action liệt kê — FE trước đó phải tự đếm/suy diễn số lượng export đã chạy, dẫn tới số "Audit Packages Exported" hiển thị sai. Action mới chỉ thêm 1 endpoint liệt kê phân trang, tái dùng `PagedResponse<T>` và `MapJobToResponse` đã có sẵn (theo đúng pattern `AuditController.GetAuditLogs`).

```
GET /api/exports?page=1&pageSize=10
Authorization: Bearer <token>
```

- **Quyền:** kế thừa `[Authorize(Roles = "Admin,Audit,Academic")]` ở class-level — không cần khai báo role riêng.
- **Response:**

```ts
interface PagedResponse<T> { items: T[]; totalCount: number; page: number; pageSize: number; }

interface ExportJobResponse {
  exportJobId: number;
  requestedByAccountId: number;
  exportType: string;         // "TrainingPackage" | "EtrPdf" | "DashboardReport" | "AttendanceReport" | "AssessmentReport" | "ClassSummary"
  fileName: string;
  filePath: string;
  status: string;             // "Completed" | "Failed" | ...
  requestedAt: string;        // ISO datetime
  completedAt: string | null;
  downloadExpiredAt: string | null;
  etrCourseRecordId: number | null;
}
```

Sắp xếp mặc định: `requestedAt` giảm dần (mới nhất trước). `page`/`pageSize` được chuẩn hoá giống `AuditController` (`page >= 1`, `pageSize` trong `[1, 100]`).

## 2. Mở rộng `GET /api/dashboard/my-dashboard` theo role

`MyDashboardResponse` giữ nguyên 8 field gốc (constructor không đổi — không phá vỡ FE hiện tại), các field mới được thêm dưới dạng **init-only property**, mặc định `null` nếu role không dùng đến.

```ts
interface MyDashboardResponse {
  // ... 8 field gốc, xem docs/maintain/2026-08-05_feature-role-based-dashboard-fe-integration-guide.md ...

  systemStats: SystemStatsSummary | null;             // Admin
  monthlyTrend: MonthlyTrendSummary | null;            // TrainingManager, Audit
  expiringStudentsCount: number | null;                // Academic
  lockedRecords: LockedRecordsSummary | null;          // Audit
  recentLockedEtrs: RecentLockedEtrSummary[] | null;   // Audit
  recentAuditLogs: AuditLogResponse[] | null;          // Audit
  recentExportJobs: ExportJobResponse[] | null;        // Audit
  evidenceSummary: EvidenceSummary | null;             // QA
  reviewedToday: number | null;                        // QA
  recentEvidenceFiles: RecentEvidenceFileSummary[] | null; // QA
  todaySessions: SessionSummary[] | null;              // Instructor
  pendingSignoffs: number | null;                      // Instructor
  profile: StudentProfileSummary | null;               // Student
  certificateSummary: CertificateSummary | null;       // Student
}

interface SystemStatsSummary {
  totalUsers: number; totalLearners: number; totalInstructors: number;
  totalCourses: number; totalClasses: number; activeAccounts: number; newUsersThisMonth: number;
}

interface MonthlyTrendSummary {
  months: string[];   // 8 phần tử, format "yyyy-MM", tăng dần theo thời gian
  locked: number[];   // cùng độ dài với months — số ETR bị khoá (IsLocked, theo tháng CompletedAt)
  returned: number[]; // số lượt trả về sửa (ApprovalHistory.NewStatus == "ReturnedForCorrection", theo tháng ActionAt)
}

interface LockedRecordsSummary { totalLocked: number; complianceRate: number; } // %, làm tròn 2 chữ số

interface RecentLockedEtrSummary {
  etrCourseRecordId: number; learnerName: string; courseName: string;
  approvedBy: string | null; completedAt: string | null;
}

interface EvidenceSummary { total: number; verified: number; pending: number; rejected: number; }

interface RecentEvidenceFileSummary {
  evidenceFileId: number; fileName: string; learnerName: string;
  verificationStatus: string; uploadedAt: string;
}

interface SessionSummary {
  sessionId: number; sessionTitle: string; classId: number; classCode: string;
  sessionDate: string | null; isConfirmed: boolean;
}

interface StudentProfileSummary { fullName: string; username: string; userCode: string; }

interface CertificateSummary {
  total: number; valid: number; expiringSoon: number; expired: number;
  recent: StudentEtrSummary[]; // tối đa 5, sắp theo expiryDate giảm dần
}
```

`InstructorClassSummary` (field `myClasses`) được mở rộng thêm 2 field:

```ts
interface InstructorClassSummary {
  classId: number; classCode: string; className: string; studentCount: number;
  attendanceRate: number;  // trung bình AttendanceRate của các SubjectResult thuộc lớp, làm tròn 2 chữ số, 0 nếu chưa có dữ liệu
  sessionCount: number;    // tổng số Session của lớp
}
```

## 3. Field nào được điền theo từng role — bảng tra cứu cho FE (bổ sung)

| Role | Field mới được điền |
|---|---|
| Admin | `systemStats` |
| TrainingManager | `monthlyTrend` |
| Academic | `lowAttendanceStudents` (không lọc theo lớp — toàn hệ thống), `expiringStudentsCount` |
| Audit | `lockedRecords`, `monthlyTrend`, `recentLockedEtrs` (tối đa 5), `recentAuditLogs` (tối đa 5), `recentExportJobs` (tối đa 5) |
| QA | `evidenceSummary`, `reviewedToday`, `recentEvidenceFiles` (tối đa 5) |
| Instructor | `myClasses[].attendanceRate/sessionCount`, `todaySessions`, `pendingSignoffs` |
| Student | `profile`, `certificateSummary` |

Các field gốc (`overview`, `statusFunnel`, `actionItems`, ...) giữ nguyên hành vi cũ theo role — xem bảng đầy đủ ở tài liệu 2026-08-05.

## 4. Định nghĩa nghiệp vụ áp dụng (vì hệ thống chưa có entity `Certificate`/`Session-signoff` riêng)

- **"Locked" / khoá hồ sơ:** `ETRCourseRecord.IsLocked == true`; tháng tính theo `CompletedAt`.
- **"Returned" (trả về sửa):** bản ghi `ApprovalHistory.NewStatus == "ReturnedForCorrection"`, tháng tính theo `ActionAt` (vì `ETRCourseRecord` không lưu thời điểm bị trả gần nhất).
- **"Chứng chỉ" (certificate) của Student:** hệ thống chưa có entity `Certificate` riêng — mỗi `ETRCourseRecord.Status == "Completed"` được coi là 1 chứng chỉ, hạn dùng theo `ExpiryDate`; `ExpiringSoon` = còn hạn nhưng `ExpiryDate` trong vòng 30 ngày tới (cùng ngưỡng với `EtrService.GetExpiringStudentsAsync`).
- **"ExpiringStudentsCount" (Academic):** tái dùng `IEtrService.GetDueForTrainingAsync(null, 30, ct)` đã có sẵn (không viết lại logic Expired/ExpiringSoon), đếm số bản ghi có `ValidityStatus == "ExpiringSoon"`.
- **"Pending Signoff" (Instructor):** `SubjectResult` thuộc `Subject` mà Instructor được phân công (`ClassSubject.InstructorAccountId`), đã có `Status` (đã chấm) nhưng chưa có `SubjectSignoff` tương ứng.
- **"ApprovedBy" (Audit, trong `recentLockedEtrs`):** người thực hiện `ApprovalHistory.ActionType == "Approve"` gần nhất cho `ApprovalRequest` của ETR đó; `null` nếu chưa từng có bước Approve nào được ghi nhận.

## 5. Đã kiểm chứng bằng cách nào

- `dotnet build` toàn solution: **0 Error** (4 warning `NU1510`/`CS8604` đều đã tồn tại từ trước, không phát sinh mới).
- `dotnet test ETR.Application.Tests`: **4/4 pass** — test mới cho `DashboardKpiCalculator.{ComputeSystemStatsAsync, ComputeMonthlyTrendAsync, ComputeLockedRecordsSummaryAsync, ComputeEvidenceSummaryAsync}`, dùng Moq mock `IUnitOfWork` theo đúng pattern sẵn có trong solution.
- Rà soát thủ công từng nhánh role trong `DashboardService.GetMyDashboardAsync` để đảm bảo field mới chỉ điền đúng role tương ứng, các role khác vẫn `null` như cũ.

## 6. Rủi ro/việc còn lại

- `recentLockedEtrs`/`recentAuditLogs`/`recentEvidenceFiles`/`recentExportJobs` hiện load toàn bộ bảng vào memory rồi `OrderByDescending().Take(5)` (giống pattern `AuditController` sẵn có) — chấp nhận được ở quy mô dữ liệu hiện tại, cần chuyển sang query có `ORDER BY ... LIMIT` ở tầng DB nếu dữ liệu audit log/evidence file tăng lớn.
- `monthlyTrend` cố định 8 tháng gần nhất (không cấu hình được qua query param) — đúng theo yêu cầu, nhưng nếu sau này cần khác 8 tháng sẽ phải sửa hằng số `MonthlyTrendMonthCount` trong `DashboardService`.
- Chưa có unit test riêng cho `DashboardService.GetMyDashboardAsync` theo từng role mới (Admin/TrainingManager/Academic/Audit/QA/Instructor/Student) — do việc mock đầy đủ `IUnitOfWork` với ~15 repository cho từng nhánh tốn nhiều thời gian hơn giá trị mang lại ở bước này; đã bù bằng test trực tiếp cho từng hàm tính toán thuần (`DashboardKpiCalculator`) là phần chứa logic nghiệp vụ chính.

## 7. Bổ sung cùng ngày: chuẩn hoá `ActionType` sang enum (theo yêu cầu review lại recommendation)

Trong lúc soát lại rủi ro nêu ở mục recommendation đi kèm, phát hiện `ActionType` được ghi bằng raw string ở **~20 file** cho 2 cột khác nhau:

- `AuditLog.ActionType` (SCREAMING_CASE: `INSERT`, `UPDATE`, `DELETE`, `SUBMIT`, `VERIFY`, `RETURN`, `APPROVE`, `LOCK`, `UNLOCK`, `ADMIN_FORCE_UNLOCK`, `AMENDMENT_REQUEST`, `AMENDMENT_APPROVE`, `AMENDMENT_REJECT`, `IMPORT_ATTENDANCE`, `IMPORT_ASSESSMENT`) — đã nhất quán casing sẵn, nhưng không có gì chặn 1 typo tạo ra 1 giá trị mới không khớp được với giá trị cũ.
- `ApprovalHistory.ActionType` (PascalCase: `Submit`, `Review`, `Verify`, `Approve`, `Reject`, `Return`) — cũng đã nhất quán, cùng rủi ro typo.

**Lưu ý đính chính:** claim ban đầu trong recommendation ("APPROVE" vs "Approve" là cùng 1 bug) là **sai** — đây là 2 cột khác nhau, mỗi cột tự nó đã nhất quán. Xem phần "CORRECTION" trong file recommendation để biết chi tiết.

**Đã làm** (không đổi DB schema — 2 cột vẫn là `nvarchar`, chỉ thay raw string literal trong code C# bằng `enum.ToString()`):
- Thêm `ETR.Domain/Enums/AuditActionType.cs` và `ETR.Domain/Enums/ApprovalHistoryActionType.cs`.
- Thay toàn bộ `ActionType = "..."` literal trong `EtrService.cs`, `UserProfileService.cs`, `CourseService.cs`, `AmendmentService.cs`, `EnrollmentService.cs`, `CompletionRequirementService.cs`, `ClassService.cs`, `AttendanceService.cs`, `ImportService.cs`, `AssessmentResultService.cs`, `EvidenceService.cs`, `AccountService.cs`, `AppDbContext.Compliance.cs`, `DataSeeder.cs` bằng `AuditActionType.X.ToString()` / `ApprovalHistoryActionType.X.ToString()`.
- `ApprovalService.cs`: thay `action.ToString()` / `action.ToString().ToUpperInvariant()` (phụ thuộc ngầm vào tên enum `ApprovalActionType` — enum contract riêng cho API `?action=`) bằng 2 dictionary map tường minh (`HistoryActionByApprovalAction`, `AuditActionByApprovalAction`) — đổi tên 1 trong 2 enum giờ sẽ gây lỗi build thay vì âm thầm lệch dữ liệu.

**Kiểm chứng:** `dotnet build` toàn solution — 0 Error; `dotnet test` — 4/4 pass; `grep -rn 'ActionType = "' --include="*.cs" .` — 0 kết quả trên toàn bộ solution.

## 8. Files liên quan

| File | Trạng thái |
|---|---|
| `ETR.API/Controllers/ExportsController.cs` | Sửa — thêm action `GET /api/exports` (phân trang) |
| `ETR.Application/DTOs/Dashboard/MyDashboardResponse.cs` | Sửa — thêm 13 DTO mới, mở rộng `MyDashboardResponse`/`InstructorClassSummary` |
| `ETR.Application/Services/DashboardKpiCalculator.cs` | Sửa — thêm `ComputeSystemStatsAsync`, `ComputeMonthlyTrendAsync`, `ComputeLockedRecordsSummaryAsync`, `ComputeEvidenceSummaryAsync` |
| `ETR.Application/Services/DashboardService.cs` | Sửa — mở rộng switch theo role, thêm các hàm `Compute*` riêng cho Auditor/QA/Instructor/Student |
| `ETR.Application.Tests/Services/DashboardKpiCalculatorTests.cs` | Mới — 4 test case cho các hàm tính toán mới |
| `ETR.Domain/Enums/AuditActionType.cs` | Mới — enum cho `AuditLog.ActionType` |
| `ETR.Domain/Enums/ApprovalHistoryActionType.cs` | Mới — enum cho `ApprovalHistory.ActionType` |
| `ETR.Application/Services/ApprovalService.cs` | Sửa — thay `ToString()`/`ToUpperInvariant()` ngầm định bằng map tường minh sang 2 enum trên |
| `ETR.Application/Services/{EtrService,UserProfileService,CourseService,AmendmentService,EnrollmentService,CompletionRequirementService,ClassService,AttendanceService,ImportService,AssessmentResultService,EvidenceService,AccountService}.cs` | Sửa — thay raw string `ActionType` bằng enum `.ToString()` |
| `ETR.Infrastructure/Data/AppDbContext.Compliance.cs` | Sửa — thay raw string `"INSERT"`/`"UPDATE"` trong audit interceptor tự động bằng enum |
| `ETR.Infrastructure/Data/DataSeeder.cs` | Sửa — thay raw string `ActionType` khi seed `ApprovalHistory` bằng enum |
