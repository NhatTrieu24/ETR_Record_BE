namespace ETR.Application.Interfaces;

public interface ICurrentUserService
{
    int? AccountId { get; }
    string? RoleName { get; }
    string? IPAddress { get; }
    string? UserAgent { get; }
}
