using ETR.Domain.Enums;

namespace ETR.Application.DTOs;

public record TrainingClassResponse(int ClassId, string ClassCode, string ClassName, int CourseId, DateTime StartDate, DateTime EndDate, string? Location, int Capacity, ClassStatus Status, List<InstructorAssignmentResponse> InstructorAssignments);
