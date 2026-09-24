using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs;

public record VerifyEmailRequest(
    [Required] string Token,
    string? Email = null);
