using ETR.Application.DTOs;

namespace ETR.Application.Interfaces;

public interface IAccountService
{
    Task<IEnumerable<AccountResponse>> GetAllAccountsAsync(CancellationToken cancellationToken = default);
    Task<AccountResponse> GetAccountByIdAsync(int accountId, CancellationToken cancellationToken = default);
    Task<AccountResponse> CreateAccountAsync(CreateAccountRequest request, int createdByAccountId, CancellationToken cancellationToken = default);
    Task UpdateAccountStatusAsync(int accountId, string status, int updatedByAccountId, CancellationToken cancellationToken = default);
    Task DeleteAccountAsync(int accountId, CancellationToken cancellationToken = default);
}
