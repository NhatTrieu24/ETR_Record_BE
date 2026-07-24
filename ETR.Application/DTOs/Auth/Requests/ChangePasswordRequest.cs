using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs;

public record ChangePasswordRequest(
    int UserId,
    [Required] string OldPassword,
    [Required, MinLength(6)] string NewPassword);
