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
    public static void EnsureInstructorOwnsClass(string? callerRoleName, int? callerAccountId, int? classInstructorAccountId)
    {
        if (!string.Equals(callerRoleName, "Instructor", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (classInstructorAccountId == null || classInstructorAccountId != callerAccountId)
        {
            throw new ForbiddenAccessException("Bạn không được phân công giảng dạy lớp này.");
        }
    }
}
