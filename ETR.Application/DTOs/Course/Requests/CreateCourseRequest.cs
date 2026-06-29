namespace ETR.Application.DTOs;

public record CreateCourseRequest(string CourseCode, string CourseName, string Description, int DurationHours, string Status);
