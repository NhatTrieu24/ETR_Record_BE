namespace ETR.Application.DTOs;

public record TrainingClassResponse(int ClassId, string ClassCode, string ClassName, int CourseId, DateTime StartDate, DateTime EndDate, string? Location, int Capacity, string Status);
