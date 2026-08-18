using ETR.Domain.Enums;

namespace ETR.Domain.Entities;

/// <summary>Structured request to reopen a single already-Signed-off SubjectResult for correction,
/// instead of an Instructor either editing frozen data directly or calling a Training Manager to
/// manually walk back the whole ETR. Status: Pending/Approved/Rejected.</summary>
public class AmendmentRequest : BaseEntity
{
    public int AmendmentRequestId { get; set; }
    public int SubjectResultId { get; set; }
    public int RequestedByAccountId { get; set; }
    public string Reason { get; set; } = string.Empty;

    /// <summary>Snapshot of SubjectResult.Status at the moment the request was created.</summary>
    public string OldValue { get; set; } = string.Empty;

    /// <summary>SubjectResult.Status it was reset to on Approve; null while Pending/Rejected.</summary>
    public string? NewValue { get; set; }

    public AmendmentStatus Status { get; set; } = AmendmentStatus.Pending;
    public int? ApprovedByAccountId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? DecisionComment { get; set; }
}
