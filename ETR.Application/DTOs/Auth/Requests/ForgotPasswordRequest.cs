using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs;

public record ForgotPasswordRequest([Required, EmailAddress] string Email);
