using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs;

public record InstructorAssignmentRequest(int SubjectId, int? InstructorAccountId);
