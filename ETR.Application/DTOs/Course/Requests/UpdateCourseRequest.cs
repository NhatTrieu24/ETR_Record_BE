namespace ETR.Application.DTOs;

public record UpdateCourseRequest(int CourseId, string CourseCode, string CourseName, string Description, int DurationHours, string Status);
