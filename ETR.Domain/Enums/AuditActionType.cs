namespace ETR.Domain.Enums;

/// <summary>Every value ever written to AuditLog.ActionType (a free-text nvarchar column — this
/// enum does not change the DB schema, it only replaces raw string literals at call sites so a typo
/// can't silently create a new, uncomparable value). Member names are intentionally SCREAMING_CASE
/// to match the strings already persisted historically, so ToString() round-trips identically and
/// existing rows stay comparable to newly written ones.</summary>
public enum AuditActionType
{
    INSERT,
    UPDATE,
    DELETE,
    SUBMIT,
    VERIFY,
    RETURN,
    APPROVE,
    REJECT,
    LOCK,
    UNLOCK,
    ADMIN_FORCE_UNLOCK,
    AMENDMENT_REQUEST,
    AMENDMENT_APPROVE,
    AMENDMENT_REJECT,
    IMPORT_ATTENDANCE,
    IMPORT_ASSESSMENT,
    IMPORT_CLASS_ROSTER
}
