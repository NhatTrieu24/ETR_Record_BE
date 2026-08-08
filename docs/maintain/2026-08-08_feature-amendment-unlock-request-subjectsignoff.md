# Feature: Amendment / Unlock-request ở cấp SubjectSignoff — 2026-08-08

**Ngày thực hiện:** 2026-08-08
**Phạm vi:** `ETR.Domain/Entities/AmendmentRequest.cs` (mới); `ETR.Infrastructure/Migrations/*_AddAmendmentRequest.cs` (mới); `ETR.Infrastructure/Data/AppDbContext.cs`, `ETR.Infrastructure/Repositories/UnitOfWork.cs`, `ETR.Application/Interfaces/IUnitOfWork.cs` (mở rộng); `ETR.Application/DTOs/Amendment/**` (mới); `ETR.Application/Interfaces/IAmendmentService.cs`, `ETR.Application/Services/AmendmentService.cs` (mới); `ETR.Application/DependencyInjection.cs` (mở rộng); `ETR.API/Controllers/SubjectSignoffController.cs` (mở rộng), `ETR.API/Controllers/AmendmentsController.cs` (mới); test mới `ETR.Application.Tests/Services/AmendmentServiceTests.cs`.
**Mục tiêu:** `/mpower:code-fix` — triển khai mục #2 trong `docs/todo/9.todo_to_complete_system.md` (Amendment/Unlock-request ở cấp SubjectSignoff), giải quyết đúng scenario concern.md đã nêu: Instructor lỡ Sign-off sai, không có quyền tự sửa, không có API xin mở khóa, phải gọi điện nhờ Training Manager xử lý thủ công.

---

## 1. Vấn đề trước khi sửa

Cơ chế "mở khóa" (Unlock) trước đây chỉ tồn tại ở **cấp toàn bộ ETRCourseRecord** (`EtrService.UnlockEtrAsync`, gọi qua `POST /api/etr/{id}/reopen`) — chỉ dùng được sau khi ETR đã `Completed`. Không có cơ chế nào để sửa **1 Subject cụ thể** đã Sign-off mà không phải mở khóa toàn bộ hồ sơ. Rà soát thêm phát hiện: thực ra hệ thống hiện tại **không hề khóa dữ liệu ở cấp Subject** — `ImmutabilityValidator` chỉ chặn sửa khi ETR đã `Completed`/`IsLocked`, nên về mặt kỹ thuật Instructor vẫn có thể chỉnh sửa SubjectResult/AssessmentResult ngay cả sau khi đã Sign-off, miễn ETR chưa Completed. Cái thiếu thực sự không phải là "mở khóa kỹ thuật", mà là **1 luồng có kiểm soát, có phê duyệt, có audit trail** để làm việc này — thay cho việc sửa lặng lẽ hoặc gọi điện thoại ngoài hệ thống.

## 2. Thiết kế

### 2.1 Entity `AmendmentRequest` — đúng theo đề xuất trong todo doc

`SubjectResultId, RequestedByAccountId, Reason, OldValue, NewValue, Status (Pending/Approved/Rejected), ApprovedByAccountId, ApprovedAt, DecisionComment`. `OldValue` là snapshot `SubjectResult.Status` tại thời điểm xin mở khóa; `NewValue` chỉ được điền khi `Approved` (luôn là `"Pending"` — giống trạng thái ban đầu của 1 SubjectResult mới tạo).

### 2.2 Phạm vi có chủ đích: CHỈ áp dụng trước khi ETR `Completed`

Amendment Request được thiết kế cho đúng tình huống concern.md mô tả: hồ sơ đang "lơ lửng" giữa quy trình (chưa Submit, hoặc đang Submitted/Verified, chưa Approve). Nếu ETR đã `Completed`/`IsLocked`, hệ thống **từ chối tạo Amendment Request mới** và hướng dẫn dùng `POST /api/etr/{id}/reopen` (cơ chế Unlock cấp ETR đã có từ trước) — tránh 2 cơ chế mở khóa chồng chéo nhau, không rõ cái nào là nguồn sự thật. Kiểm tra này chạy lại **2 lần**: lúc tạo request VÀ lúc Approve (đề phòng ETR bị Complete bởi người khác trong lúc request đang Pending).

### 2.3 Luồng nghiệp vụ

```
Instructor: POST /api/subjectsignoff/{subjectResultId}/unlock-request  { reason }
              → tạo AmendmentRequest (Status=Pending), ghi AuditLog

Training Manager: POST /api/amendments/{id}/approve  { comment? }
              → SubjectResult.Status = "Pending" (mở lại)
              → TOÀN BỘ SubjectSignoff cũ của Subject đó bị soft-delete (vô hiệu hóa chữ ký cũ)
              → AmendmentRequest.Status = "Approved", ghi AuditLog
              → Instructor sửa lại Attendance/Assessment/Evidence, rồi Sign-off lại từ đầu

           HOẶC  POST /api/amendments/{id}/reject  { comment (bắt buộc) }
              → SubjectResult giữ nguyên, AmendmentRequest.Status = "Rejected", ghi AuditLog
```

### 2.4 Vì sao soft-delete SubjectSignoff cũ khi Approve, không phải sửa/xóa cứng

`EtrService.SubmitEtrAsync` kiểm tra `hasSignoff = allSignoffs.Any(s => s.SubjectResultId == sr.SubjectResultId)` để chặn Submit khi Subject chưa được ký. Nếu giữ nguyên chữ ký cũ (đã sai) sau khi Approve Amendment, `hasSignoff` vẫn `true` — hệ thống sẽ cho Submit dù Instructor chưa thực sự Sign-off lại bản sửa đúng. Soft-delete (không phải hard-delete, đúng nguyên tắc `BaseEntity.IsDeleted` toàn hệ thống + Audit Trail) khiến `hasSignoff` tự động về `false` (nhờ global query filter theo `IsDeleted` đã có sẵn ở `AppDbContext`), buộc Instructor phải Sign-off lại thật.

## 3. API mới

```
POST /api/subjectsignoff/{subjectResultId}/unlock-request
Authorization: Instructor, Academic, Admin
Body: { "reason": "Chấm nhầm điểm bài thi lý thuyết" }

POST /api/amendments/{id}/approve
Authorization: TrainingManager, Admin
Body: { "comment": "Đồng ý, sửa lại điểm" }   // optional

POST /api/amendments/{id}/reject
Authorization: TrainingManager, Admin
Body: { "comment": "Điểm cũ đã đúng, không cần sửa" }   // bắt buộc

GET /api/amendments            — Instructor, Academic, TrainingManager, Admin
GET /api/amendments/{id}       — Instructor, Academic, TrainingManager, Admin
```

## 4. Business rules đã enforce

1. Không cho tạo Amendment Request nếu SubjectResult **chưa** có SubjectSignoff nào (chưa ký thì cứ sửa trực tiếp, không cần xin phép).
2. Không cho tạo Amendment Request mới nếu Subject đó đang có 1 request `Pending` khác (tránh spam nhiều request cho cùng 1 subject).
3. Không cho tạo/Approve nếu ETR cha đã `Completed`/`IsLocked` — dùng `POST /etr/{id}/reopen` thay thế.
4. Reject bắt buộc có `Comment` (giải trình lý do, cùng convention với Return/Reject ở `ApprovalService`).
5. Không cho Approve/Reject 1 request đã được quyết định trước đó (`Status != "Pending"`).
6. Mọi request/approve/reject đều ghi `AuditLog` với `OldValue`/`NewValue` đầy đủ.

## 5. Việc CHƯA làm / rủi ro đã biết

- **Chưa đồng bộ 3 file raw SQL deploy** (`Deploy_ETR_System.sql`, `ALL_IN_ONE_Deploy.sql`, `Deploy_NukeAndSeed.sql`) với bảng `AmendmentRequests` mới — các file này chỉ dùng cho reset DB thủ công ngoài `dotnet ef` (theo `stack.md`), migration EF Core (`dotnet ef database update`) đã đủ cho luồng phát triển/CI bình thường. Nếu team có quy trình reset DB bằng SQL thủ công, cần bổ sung `CREATE TABLE [AmendmentRequests]` vào các file này trước khi dùng — chưa làm trong lượt này để tránh sửa nhầm 3 file SQL lớn không có test bảo vệ.
- **Không giới hạn Instructor chỉ được request cho Subject do chính mình Sign-off** — hiện tại bất kỳ ai có role Instructor/Academic/Admin đều gọi được `unlock-request` cho bất kỳ SubjectResultId nào, khớp với mô hình phân quyền coarse-grained (role-based, không identity-based) đã dùng nhất quán trong toàn codebase (VD: `EtrController` cũng không kiểm tra "phải là người tạo ETR"). Nếu cần siết chặt hơn, đây là điểm cần bổ sung riêng.
- **Chỉ reset `SubjectResult.Status`, không tự động unpublish `AssessmentResult` liên quan** — nếu lý do sửa là điểm số đã `IsPublished=true`, Instructor vẫn cần dùng luồng "Retake" sẵn có (ghi nhận lần thi lại mới, không đè điểm cũ) để thực sự sửa được điểm — đúng theo nguyên tắc immutability điểm đã publish (`ImmutabilityValidator.ValidatePublishedAssessmentScore`), Amendment không (và không nên) bypass riêng nguyên tắc này.

## 6. Đã kiểm chứng bằng cách nào

- **TDD**: viết test trước cho từng nhánh hành vi (chưa ký → chặn tạo request; ETR đã Completed → chặn tạo và chặn approve; đã có request Pending → chặn tạo trùng; tạo hợp lệ → Pending + AuditLog; Approve hợp lệ → reset SubjectResult + vô hiệu hóa chữ ký cũ + AuditLog; Approve khi ETR vừa Completed lúc đang Pending → chặn; Approve/Reject request đã quyết định → chặn; Reject thiếu comment → chặn; Reject hợp lệ → giữ nguyên SubjectResult) → xác nhận RED (build lỗi vì `IAmendmentService` chưa tồn tại) → implement → xác nhận GREEN.
- `dotnet build` toàn solution: **0 Error** (2 warning pre-existing, không liên quan).
- `dotnet ef migrations add AddAmendmentRequest`: tạo migration thành công, build lại sau khi có migration: **0 Error**.
- `dotnet test`: **47/47 pass** (38 cũ + 9 mới), không regression.

## 7. Files liên quan

| File | Trạng thái |
|---|---|
| `ETR.Domain/Entities/AmendmentRequest.cs` | Mới |
| `ETR.Infrastructure/Migrations/20260808044243_AddAmendmentRequest.cs` (+ `.Designer.cs`) | Mới |
| `ETR.Infrastructure/Data/AppDbContext.cs` | Sửa — thêm `DbSet<AmendmentRequest>` |
| `ETR.Application/Interfaces/IUnitOfWork.cs`, `ETR.Infrastructure/Repositories/UnitOfWork.cs` | Sửa — thêm `AmendmentRequestRepository` |
| `ETR.Application/DTOs/Amendment/Requests/CreateAmendmentRequestRequest.cs` | Mới |
| `ETR.Application/DTOs/Amendment/Requests/DecideAmendmentRequestRequest.cs` | Mới |
| `ETR.Application/DTOs/Amendment/Responses/AmendmentRequestResponse.cs` | Mới |
| `ETR.Application/Interfaces/IAmendmentService.cs` | Mới |
| `ETR.Application/Services/AmendmentService.cs` | Mới |
| `ETR.Application/DependencyInjection.cs` | Sửa — đăng ký `IAmendmentService` |
| `ETR.API/Controllers/SubjectSignoffController.cs` | Sửa — thêm `POST {subjectResultId}/unlock-request` |
| `ETR.API/Controllers/AmendmentsController.cs` | Mới — `GET`, `GET/{id}`, `POST/{id}/approve`, `POST/{id}/reject` |
| `ETR.Application.Tests/Services/AmendmentServiceTests.cs` | Mới — 9 test case |
| `docs/todo/9.todo_to_complete_system.md`, `ETR.Documentation/final/9.todo_to_complete_system.md` | Sửa — đánh dấu mục #2 đã triển khai |
