namespace ETR.Domain.Enums;

/// <summary>Every value ever written to ApprovalHistory.ActionType (a free-text nvarchar column —
/// this enum does not change the DB schema, it only replaces raw string literals at call sites).
/// Member names are PascalCase to match the strings already persisted historically (both
/// ApprovalService's <c>ApprovalActionType.ToString()</c> and EtrService's direct writes already
/// used this casing), so ToString() round-trips identically. "Review" is seed-data-only (DataSeeder
/// simulates an intermediate QA review step that no production code path currently writes).</summary>
public enum ApprovalHistoryActionType
{
    Submit,
    Review,
    Verify,
    Approve,
    Reject,
    Return
}
