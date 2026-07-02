namespace ETR.Application.DTOs;

public record AuthResponse(int UserId, string Username, string FullName, string Token, string RefreshToken);
