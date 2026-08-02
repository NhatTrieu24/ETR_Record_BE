using ETR.Application.DTOs;

namespace ETR.Application.Interfaces;

public interface IAccountService
{
    Task<IEnumerable<AccountResponse>> GetAllAccountsAsync(CancellationToken cancellationToken = default);
    Task<AccountResponse> GetAccountByIdAsync(int accountId, CancellationToken cancellationToken = default);
    Task<AccountResponse> CreateAccountAsync(CreateAccountRequest request, int createdByAccountId, bool isCallerAdmin, CancellationToken cancellationToken = default);
    Task UpdateAccountStatusAsync(int accountId, string status, int updatedByAccountId, CancellationToken cancellationToken = default);
    Task UpdateAccountRoleAsync(int accountId, int roleId, int updatedByAccountId, CancellationToken cancellationToken = default);
    Task DeleteAccountAsync(int accountId, int deletedByAccountId, CancellationToken cancellationToken = default);
}
