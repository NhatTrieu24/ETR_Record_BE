namespace ETR.Application.DTOs;

public record ClassStudentResponse(
    int ClassStudentId,
    int CourseEnrollmentId,
    int ClassId,
    int AccountId,
    string Status
);
