namespace ETR.Application.Interfaces;

public interface ICurrentUserService
{
    int? UserId { get; }
    string? RoleName { get; }
}
