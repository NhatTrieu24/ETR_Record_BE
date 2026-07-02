namespace ETR.Application.DTOs;

public record AssignInstructorRequest(int ClassId, int UserId, bool IsPrimaryInstructor);
