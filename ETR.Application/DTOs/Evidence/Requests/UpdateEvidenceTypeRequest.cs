namespace ETR.Application.DTOs;

public record UpdateEvidenceTypeRequest(int EvidenceTypeId, string TypeName, string? Description);
