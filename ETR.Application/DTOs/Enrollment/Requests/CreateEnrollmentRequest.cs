using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs;

public record CreateEnrollmentRequest(
    [Range(1, int.MaxValue)] int AccountId, 
    [Range(1, int.MaxValue)] int ClassId);
