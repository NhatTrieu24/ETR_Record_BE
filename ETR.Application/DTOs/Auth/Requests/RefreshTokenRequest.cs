using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs;

public record RefreshTokenRequest([Required] string RefreshToken);
