namespace ETR.Application.Compliance;

/// <summary>
/// "Sân nhà ai nấy đá" (Data Isolation / Resource-based Authorization) — team decision 2026-08-08,
/// docs/todo/addition.md. An Instructor may only write data (Attendance, AssessmentResult,
/// Evidence, SubjectSignoff) into a Class they are actually assigned to (Class.InstructorAccountId).
/// Every other role (Admin, Academic, TrainingManager, QA, Audit) is unrestricted by this check —
/// [Authorize(Roles=...)] at the controller already governs which of those roles may call the
/// action at all; this validator only adds the extra identity/ownership layer Instructor needs.
/// </summary>
public static class ClassOwnershipValidator
{
    public static void EnsureInstructorOwnsSubject(string? callerRoleName, bool isInstructorAssignedToSubject)
    {
        if (!string.Equals(callerRoleName, "Instructor", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!isInstructorAssignedToSubject)
        {
            throw new ForbiddenAccessException("Bản không được phân công giảng dạy môn này trong lớp.");
        }
    }
}
