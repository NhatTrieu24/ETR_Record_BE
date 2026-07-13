namespace ETR.Application.DTOs;

public record LoginResponseDto(int UserId, string Username, string FullName, string RoleName, string Token);
