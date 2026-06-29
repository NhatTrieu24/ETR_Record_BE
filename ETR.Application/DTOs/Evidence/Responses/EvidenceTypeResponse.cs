namespace ETR.Application.DTOs;

public record EvidenceTypeResponse(
    int EvidenceTypeId,
    string TypeName,
    string? Description);
