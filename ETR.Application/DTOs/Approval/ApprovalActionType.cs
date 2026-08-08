namespace ETR.Application.DTOs.Approval;

/// <summary>
/// Shared FE-BE contract for POST /api/approvals/{id}/process?action=... — replaces the previous
/// raw `string action` query param. Because this is a real enum type (not a free string), ASP.NET
/// Core model binding rejects any value outside these 4 with a clean 400 before the request even
/// reaches ApprovalService, and Swagger/OpenAPI now emits it as a proper enum the FE can generate
/// typed clients from, instead of an untyped string.
/// </summary>
public enum ApprovalActionType
{
    Verify,
    Approve,
    Reject,
    Return
}
