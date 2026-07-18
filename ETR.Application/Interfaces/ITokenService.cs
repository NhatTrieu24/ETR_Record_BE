using ETR.Domain.Entities;

namespace ETR.Application.Interfaces;

public interface ITokenService
{
    string GenerateToken(Account account, Role role);
}
