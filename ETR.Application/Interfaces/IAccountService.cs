using ETR.Application.DTOs;
using ETR.Domain.Enums;

namespace ETR.Application.Interfaces;

public interface IAccountService
{
    Task<IEnumerable<AccountResponse>> GetAllAccountsAsync(CancellationToken cancellationToken = default);
    Task<AccountResponse> GetAccountByIdAsync(int accountId, CancellationToken cancellationToken = default);
    Task<AccountResponse> CreateAccountAsync(CreateAccountRequest request, int createdByAccountId, bool isCallerAdmin, CancellationToken cancellationToken = default);
    Task UpdateAccountStatusAsync(int accountId, AccountStatus status, int updatedByAccountId, CancellationToken cancellationToken = default);
    Task UpdateAccountRoleAsync(int accountId, int roleId, int updatedByAccountId, CancellationToken cancellationToken = default);
    Task UpdateAccountDepartmentAsync(int accountId, int departmentId, int updatedByAccountId, CancellationToken cancellationToken = default);
    Task DeleteAccountAsync(int accountId, int deletedByAccountId, CancellationToken cancellationToken = default);
}
