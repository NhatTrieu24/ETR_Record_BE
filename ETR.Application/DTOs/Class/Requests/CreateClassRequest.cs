namespace ETR.Application.DTOs;

public record CreateClassRequest(string ClassCode, string ClassName, int CourseId, DateTime StartDate, DateTime EndDate, string? Location, int Capacity, string Status);
