namespace ETR.Application.DTOs;

// Data source for the "reminder when attendance rate is low" gap (M4) — this repo has no email/push
// notification infrastructure, so this endpoint surfaces the underlying data (who's below threshold,
// in which subject) for the frontend to build a reminder UI on top of, rather than sending anything itself.
public record LowAttendanceStudentResponse(
    int AccountId,
    string UserCode,
    string FullName,
    int ClassId,
    string ClassCode,
    int SubjectId,
    string SubjectCode,
    decimal AttendanceRate,
    decimal ThresholdPercent);
