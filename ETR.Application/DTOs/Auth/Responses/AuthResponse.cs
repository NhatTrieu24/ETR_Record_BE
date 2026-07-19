namespace ETR.Application.DTOs;

public record AuthResponse(int AccountId, string Username, string FullName, string Role, string Token, string RefreshToken);
