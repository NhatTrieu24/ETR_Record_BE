using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs;

public record SendEmailVerificationRequest(
    [Required, EmailAddress] string Email,
    string? ClientBaseUrl = null);
