# Feature: Bulk-verify Evidence — 2026-08-08

**Ngày thực hiện:** 2026-08-08
**Phạm vi:** `ETR.Application/DTOs/Evidence/Requests/BulkVerifyEvidenceRequest.cs` (mới); `ETR.Application/DTOs/Evidence/Responses/BulkVerifyEvidenceResponse.cs` (mới); `ETR.Application/Interfaces/IEvidenceService.cs` (mở rộng); `ETR.Application/Services/EvidenceService.cs` (mở rộng + refactor); `ETR.API/Controllers/EvidencesController.cs` (1 endpoint mới); test mới/mở rộng `ETR.Application.Tests/Services/EvidenceServiceTests.cs`.
**Mục tiêu:** `/mpower:code-fix` — triển khai mục #1 trong `docs/todo/9.todo_to_complete_system.md` (Bulk-verify Evidence), giải quyết nút thắt cổ chai QA phải verify từng file minh chứng một (VD: 1 lớp 30 học viên × 3 file = 90 lượt click).

---

## 1. Vấn đề trước khi sửa

`EvidencesController` chỉ có `PUT /{id}/verify` — verify đúng 1 file/lần gọi. QA muốn duyệt cả lớp phải lặp lại thao tác hàng chục, hàng trăm lần — nghiệp vụ thực tế dễ dẫn tới QA verify ẩu để giảm tải, làm giảm chất lượng kiểm định. Ngoài ra, `VerifyEvidenceAsync` gốc **không ghi AuditLog** — chỉ `DeleteEvidenceAsync` có, nghĩa là hành động quan trọng nhất của QA (verify/reject minh chứng) trước đây hoàn toàn không có dấu vết kiểm toán.

## 2. Thiết kế

### 2.1 1 status/comment chung cho cả batch, không phải per-item

QA khi bulk-verify thường đang xử lý cùng 1 quyết định cho nhiều file (VD: "tất cả minh chứng của lớp X hôm nay đều Verified"), không phải chọn riêng từng trạng thái cho từng file — nên request nhận `List<int> EvidenceIds` + **1** `VerificationStatus`/`VerificationComment` áp dụng chung cho toàn batch, khớp đúng thiết kế đã đề ra trong todo doc (`PUT /api/evidences/bulk-verify` nhận `List<int> EvidenceIds` + `VerificationStatus` + `VerificationComment` chung).

### 2.2 1 item lỗi không rollback cả batch — response tách `Verified`/`Failed`

Nếu 1 trong 90 file bị lỗi (không tồn tại, hoặc QA vô tình chọn nhầm file do chính họ tải lên), toàn bộ batch không nên thất bại — QA sẽ mất công verify lại từ đầu 89 file đã đúng. `BulkVerifyEvidenceResponse` trả về `Verified: List<EvidenceResponse>` (đã xử lý thành công) và `Failed: List<BulkVerifyFailureItem(EvidenceFileId, Reason)>` (lỗi kèm lý do) — QA đọc `Failed` để biết chính xác cần xử lý tay file nào.

Validation áp dụng cho TOÀN BATCH (status hợp lệ, có comment khi Reject) vẫn throw ngay từ đầu nếu sai — vì đây là lỗi request, không phải lỗi từng item.

### 2.3 Refactor: gộp logic verify dùng chung cho cả single và bulk

`VerifyEvidenceAsync` (single) và `BulkVerifyEvidencesAsync` giờ dùng chung 3 private helper mới trong `EvidenceService`:
- `ValidateVerificationRequest(status, comment)` — validate status hợp lệ + comment bắt buộc khi Reject.
- `EnsureVerifierDidNotUploadEvidence(evidence, verifiedByAccountId)` — segregation-of-duties check (không đổi hành vi cũ).
- `ApplyVerificationAsync(evidence, status, comment, verifiedByAccountId, ct)` — áp dụng thay đổi lên entity **và ghi 1 AuditLog entry**.

Vì `ApplyVerificationAsync` ghi AuditLog, tác dụng phụ **tích cực** của refactor này là `VerifyEvidenceAsync` (single-verify) giờ cũng có AuditLog — trước đây không có. Đây không phải scope creep: bulk cần đúng logic per-item mà single đã có, tách chung ra là cách duy nhất để không lặp code, và audit trail đầy đủ cho cả 2 đường là đúng với nguyên tắc "mọi hành động verify phải có audit trail" đã áp dụng nhất quán ở các module khác (Approval, ETR).

## 3. API mới

```
PUT /api/evidences/bulk-verify
Authorization: QA, Admin

{
  "evidenceIds": [101, 102, 103],
  "verificationStatus": "Verified",
  "verificationComment": null
}
```

Response:
```json
{
  "verified": [ { "evidenceFileId": 101, "verificationStatus": "Verified", ... }, ... ],
  "failed": [ { "evidenceFileId": 999, "reason": "Evidence not found." } ]
}
```

## 4. Rủi ro/giới hạn đã biết (chưa nằm trong scope task này)

- **Không kiểm tra phạm vi lớp học của QA** — todo doc gốc có nêu "Validate tất cả EvidenceId thuộc đúng phạm vi lớp mà QA Staff được phép xử lý", nhưng rà soát toàn bộ codebase xác nhận **không có khái niệm QA-scoped-by-class ở bất kỳ đâu** (khác với Instructor, luôn được scope theo `InstructorAccountId` trên `Class`) — `VerifyEvidence` (cả single và bulk) hiện cho phép bất kỳ QA nào verify bất kỳ Evidence nào, y hệt hành vi single-verify cũ. Việc thêm scoping mới cho QA là 1 thay đổi mô hình phân quyền (theo Department? theo Course? theo Class do QA "phụ trách"?) cần quyết định nghiệp vụ riêng trước khi code — không tự suy đoán mô hình trong lượt sửa này để tránh áp đặt sai. Đề xuất: nếu cần, mở 1 task riêng để định nghĩa mô hình QA scoping trước khi áp cho cả single và bulk verify.
- Batch không giới hạn số lượng `EvidenceIds` tối đa — chấp nhận được ở quy mô hiện tại (nhất quán với các API khác trong codebase không phân trang), nhưng có thể cần giới hạn (VD: 200 item/lần gọi) nếu dữ liệu tăng lớn.

## 5. Đã kiểm chứng bằng cách nào

- **TDD**: viết test trước cho từng nhánh hành vi (tất cả hợp lệ, có item lỗi không rollback phần còn lại, validate throw trước khi đụng tới bất kỳ item nào, single-verify giờ có AuditLog) → xác nhận RED (build lỗi vì method chưa tồn tại trên `IEvidenceService`) → implement → xác nhận GREEN.
- `dotnet build` toàn solution: **0 Error** (2 warning pre-existing, không liên quan).
- `dotnet test`: **38/38 pass** (34 cũ + 4 mới), không regression — bao gồm 2 test single-verify cũ vẫn pass nguyên vẹn sau refactor.
- Test case đáng chú ý: `BulkVerifyEvidencesAsync_OneItemSelfUploadedAndOneMissing_StillVerifiesTheRest` xác nhận đúng hành vi partial-success (2/3 item lỗi, 1 item vẫn verify thành công, đúng 1 AuditLog được ghi cho item thành công); `BulkVerifyEvidencesAsync_RejectedWithoutComment_ThrowsValidationExceptionBeforeTouchingAnyItem` xác nhận validate request-level chặn TRƯỚC khi đụng vào item nào (không có AuditLog nào bị ghi nhầm khi request sai).

## 6. Files liên quan

| File | Trạng thái |
|---|---|
| `ETR.Application/DTOs/Evidence/Requests/BulkVerifyEvidenceRequest.cs` | Mới |
| `ETR.Application/DTOs/Evidence/Responses/BulkVerifyEvidenceResponse.cs` | Mới |
| `ETR.Application/Interfaces/IEvidenceService.cs` | Sửa — thêm `BulkVerifyEvidencesAsync` |
| `ETR.Application/Services/EvidenceService.cs` | Sửa — thêm bulk-verify + refactor `VerifyEvidenceAsync` dùng chung helper, thêm AuditLog cho cả 2 đường |
| `ETR.API/Controllers/EvidencesController.cs` | Sửa — thêm `PUT bulk-verify` |
| `ETR.Application.Tests/Services/EvidenceServiceTests.cs` | Sửa — thêm 4 test case mới, cập nhật `BuildService` để mock `IAuditLogRepository` |
| `docs/todo/9.todo_to_complete_system.md`, `ETR.Documentation/final/9.todo_to_complete_system.md` | Sửa — đánh dấu mục #1 đã triển khai |
