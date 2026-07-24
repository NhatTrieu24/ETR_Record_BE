using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs;

public record ResetPasswordRequest(
    [Required] string Token,
    [Required, MinLength(6)] string NewPassword);
