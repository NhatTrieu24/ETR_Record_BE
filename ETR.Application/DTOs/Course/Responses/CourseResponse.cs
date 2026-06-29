namespace ETR.Application.DTOs;

public record CourseResponse(int CourseId, string CourseCode, string CourseName, string Description, int DurationHours, string Status);
