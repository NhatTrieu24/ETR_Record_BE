namespace ETR.Application.DTOs;

public record ClassInstructorResponse(int ClassInstructorId, int ClassId, int UserId, bool IsPrimaryInstructor, DateTime AssignedAt);
