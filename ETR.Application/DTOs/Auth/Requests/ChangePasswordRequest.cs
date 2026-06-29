namespace ETR.Application.DTOs;

public record ChangePasswordRequest(int UserId, string OldPassword, string NewPassword);
