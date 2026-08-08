namespace ETR.Application.DTOs.Evidence;

public record BulkVerifyFailureItem(int EvidenceFileId, string Reason);

public record BulkVerifyEvidenceResponse(List<EvidenceResponse> Verified, List<BulkVerifyFailureItem> Failed);
