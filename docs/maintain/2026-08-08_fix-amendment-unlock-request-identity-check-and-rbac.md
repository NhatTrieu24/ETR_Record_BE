# Fix: Amendment/Unlock-request — Identity Check, Loại Academic, Admin Force Unlock Audit — 2026-08-08

**Ngày thực hiện:** 2026-08-08
**Phạm vi:** `ETR.Application/Interfaces/IAmendmentService.cs`, `ETR.Application/Services/AmendmentService.cs` (sửa); `ETR.API/Controllers/SubjectSignoffController.cs` (sửa); test mở rộng `ETR.Application.Tests/Services/AmendmentServiceTests.cs`.
**Mục tiêu:** `/mpower:code-fix` — áp dụng 3 quyết định nghiệp vụ team chốt trong buổi họp 2026-08-08 (`docs/todo/addition.md`) vào tính năng Amendment/Unlock-request đã triển khai trước đó (`docs/maintain/2026-08-08_feature-amendment-unlock-request-subjectsignoff.md`), đồng thời rà soát tình trạng đồng bộ SQL deploy thủ công.

---

## 1. Vấn đề trước khi sửa

Lượt triển khai Amendment/Unlock-request trước đó (`docs/maintain/2026-08-08_feature-amendment-unlock-request-subjectsignoff.md`) đã tự nêu rõ 2 điểm còn để ngỏ, chờ team quyết định:
- "Chưa giới hạn theo danh tính người ký" — bất kỳ Instructor/Academic/Admin nào cũng gọi được `unlock-request` cho bất kỳ SubjectResultId nào.
- Không phân biệt Admin can thiệp vào chữ ký của chính mình hay của người khác trong Audit Log.

Team đã họp và trả lời dứt khoát cả 2 điểm này, đồng thời bổ sung thêm 1 luật mới (loại Academic khỏi quyền gọi) — xem `docs/todo/addition.md`.

## 2. Thiết kế — 3 thay đổi

### 2.1 Chỉ chính người đã ký mới được xin mở khóa (Instructor)

`AmendmentService.CreateAmendmentRequestAsync` giờ nhận thêm tham số `requestedByRoleName`, lấy `SubjectSignoff` **gần nhất** của Subject đó (đã soft-delete các chữ ký cũ bị Amendment trước đó vô hiệu hóa, nên `GetAllAsync` tự động chỉ trả về chữ ký đang hiệu lực nhờ global query filter theo `IsDeleted`), rồi so sánh `currentSignoff.SignoffByAccountId` với `requestedByAccountId`:

```csharp
var isOriginalSigner = currentSignoff.SignoffByAccountId == requestedByAccountId;
var isAdmin = string.Equals(requestedByRoleName, "Admin", StringComparison.OrdinalIgnoreCase);
if (!isOriginalSigner && !isAdmin)
{
    throw new ForbiddenAccessException("Bạn không có quyền can thiệp vào chữ ký của người khác — chỉ người đã Sign-off Subject này mới được xin mở khóa.");
}
```

Instructor không khớp `SignoffByAccountId` → 403 Forbidden ngay lập tức, không tạo `AmendmentRequest`, không ghi Audit Log (tránh rác log cho các lần thử sai).

### 2.2 Academic bị loại khỏi quyền gọi endpoint

`SubjectSignoffController` có `[Authorize(Roles = "Instructor,Academic,Admin")]` ở cấp class cho các action khác (GetAllSignoffs, SignoffSubjectResult, CheckSubjectSignoff — Academic vẫn cần các quyền này cho nghiệp vụ khác). Riêng action `RequestUnlock` được thêm `[Authorize(Roles = "Instructor,Admin")]` ở cấp method — theo đúng cơ chế đã ghi chú ở `EtrController` trước đây: **2 attribute Authorize ở class và method kết hợp theo AND, không phải OR**, nên method-level thu hẹp đúng ý muốn (Academic pass được class-level nhưng fail method-level → vẫn bị chặn ở action này).

### 2.3 Admin Force Unlock — Audit Log riêng biệt, mức độ nghiêm trọng cao

Khi `isAdmin && !isOriginalSigner` (Admin can thiệp vào chữ ký KHÔNG phải của chính họ), `AmendmentService` ghi `AuditLog` với `ActionType = "ADMIN_FORCE_UNLOCK"` (khác hẳn `"AMENDMENT_REQUEST"` dùng cho các yêu cầu tự thân bình thường) và `Description` bắt đầu bằng `[CẢNH BÁO]`, nêu rõ cả `AccountId` của Admin lẫn `AccountId` của người đã ký gốc — để Auditor lọc log theo `ActionType` là thấy ngay toàn bộ các lần Admin phá vỡ chữ ký người khác, không lẫn với hàng nghìn amendment request bình thường.

Nếu Admin **chính là** người đã ký (Admin cũng được phép Sign-off theo cấu hình role hiện tại), đây vẫn là yêu cầu tự thân bình thường — ghi `ActionType = "AMENDMENT_REQUEST"` như cũ, không bị coi là Force Unlock.

## 3. API thay đổi

```
POST /api/subjectsignoff/{subjectResultId}/unlock-request
Authorization: Instructor (chỉ chính người đã ký), Admin (mọi trường hợp — Force Unlock nếu không phải người ký)

# Academic gọi → 403 Forbidden (role không đủ quyền, chặn từ tầng [Authorize])
# Instructor không phải người ký → 403 Forbidden (chặn ở tầng service, có thông báo rõ lý do)
# Admin không phải người ký → 200 OK, nhưng ghi AuditLog ActionType=ADMIN_FORCE_UNLOCK
```

## 4. Rà soát riêng: đồng bộ SQL deploy thủ công

Kiểm tra lại 3 file `Deploy_ETR_System.sql`, `ALL_IN_ONE_Deploy.sql`, `Deploy_NukeAndSeed.sql` theo yêu cầu — phát hiện các file này **không chỉ thiếu bảng `AmendmentRequests`**, mà đang lạc hậu tới **9 migration** so với `ETR.Infrastructure/Migrations/` hiện tại:

| File | Migration mới nhất có trong file | Migration mới nhất thực tế |
|---|---|---|
| `Deploy_ETR_System.sql` | `20260719063637_SeedSystemData` | `20260808051722_AddCourseAndCompletionRequirementVersioning` |
| `ALL_IN_ONE_Deploy.sql` | `20260719015135_CleanUpAndSeedV` | (như trên) |
| `Deploy_NukeAndSeed.sql` | `20260719015135_CleanUpAndSeedV` | (như trên) |

9 migration bị thiếu bao gồm cả những thay đổi lớn khác không liên quan Amendment: `AddCertificateValidityAndRecurrent`, `AddInstructorAccountIdToClass`, `AddUserProfileStatusAndRemoveDashboardSnapshot`, `AddAssessmentIdToSession`, `AddPracticalChecklistIdToSession`, `AddCourseAndCompletionRequirementVersioning`, v.v.

**Quyết định (đã hỏi lại người yêu cầu trước khi làm)**: **KHÔNG** vá thêm 1 mình bảng `AmendmentRequests` vào 3 file này. Vá lẻ 1 bảng vào 1 file đã lạc hậu 9 migration sẽ tạo cảm giác sai là "file đã cập nhật" trong khi chạy thực tế vẫn lỗi ngay ở cột/bảng khác còn thiếu — tệ hơn cả việc để nguyên trạng vì đánh lừa người đọc. **Khuyến nghị cho team**: dùng `dotnet ef migrations script --idempotent` (chạy trong `ETR.Infrastructure`, `--startup-project ../ETR.API`) để **sinh lại toàn bộ** script SQL từ đầu tới migration mới nhất, thay vì tiếp tục bảo trì tay 3 file này — đây là cách duy nhất đảm bảo không sót migration nào trong tương lai.

## 5. Đã kiểm chứng bằng cách nào

- **TDD**: viết test trước cho từng nhánh hành vi (Instructor không phải người ký → Forbidden, không tạo record, không ghi audit; Admin không phải người ký → thành công + `ADMIN_FORCE_UNLOCK`; Admin chính là người ký → thành công + `AMENDMENT_REQUEST` bình thường, không bị nhầm thành Force Unlock) → xác nhận RED (build lỗi vì đổi signature `CreateAmendmentRequestAsync`) → implement → xác nhận GREEN.
- `dotnet build` toàn solution: **0 Error** (2 warning pre-existing, không liên quan).
- `dotnet test`: **56/56 pass** (53 cũ + 3 mới: `AmendmentServiceTests` từ 9 lên 12 case), không regression — 4 test case cũ của Amendment (ParentEtrAlreadyCompleted, AlreadyHasPendingRequest, ValidRequest, ApproveAmendmentRequestAsync...) đã được cập nhật dữ liệu test (`SignoffByAccountId` khớp `requestedByAccountId`) để không bị chặn bởi check identity mới, đúng phản ánh hành vi thật khi Instructor tự xin sửa hồ sơ do chính mình ký.

## 6. Files liên quan

| File | Trạng thái |
|---|---|
| `ETR.Application/Interfaces/IAmendmentService.cs` | Sửa — thêm tham số `requestedByRoleName` |
| `ETR.Application/Services/AmendmentService.cs` | Sửa — thêm identity check, phân nhánh Audit Log cho Admin Force Unlock |
| `ETR.API/Controllers/SubjectSignoffController.cs` | Sửa — thêm `[Authorize(Roles = "Instructor,Admin")]` narrow cho `RequestUnlock`, truyền `RoleName` |
| `ETR.Application.Tests/Services/AmendmentServiceTests.cs` | Sửa — thêm 3 test case mới, cập nhật dữ liệu test cho 4 case cũ |
| `docs/todo/9.todo_to_complete_system.md`, `ETR.Documentation/final/9.todo_to_complete_system.md` | Sửa — đánh dấu 3/4 điểm đã xử lý, ghi rõ tình trạng SQL deploy thủ công (không vá lẻ, khuyến nghị regenerate) |

**Không đổi / KHÔNG cần code (đã chốt nghiệp vụ, giữ nguyên):**
- QA vẫn KHÔNG bị giới hạn theo phạm vi course/class khi verify Evidence (mục 1.1 trong yêu cầu) — quyết định họp team, không có thay đổi code nào.
