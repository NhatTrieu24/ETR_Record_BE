namespace ETR.Application.DTOs;

public record SubjectResultResponse(
    int SubjectResultId,
    int SubjectId,
    string Status,
    DateTime CreatedAt,
    decimal? AttendanceRate = null);
