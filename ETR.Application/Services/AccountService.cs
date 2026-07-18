using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;

namespace ETR.Application.Services;

public class AccountService : IAccountService
{
    private readonly IUnitOfWork _unitOfWork;

    public AccountService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<AccountResponse>> GetAllAccountsAsync(CancellationToken cancellationToken = default)
    {
        var accounts = await _unitOfWork.AccountRepository.GetAllAsync(cancellationToken);
        return accounts.Select(a => new AccountResponse(a.AccountId, a.Username, a.RoleId, a.DepartmentId, a.Status));
    }

    public async Task<AccountResponse> GetAccountByIdAsync(int accountId, CancellationToken cancellationToken = default)
    {
        var account = await _unitOfWork.AccountRepository.GetByIdAsync(accountId, cancellationToken)
            ?? throw new KeyNotFoundException($"Account {accountId} not found.");
            
        return new AccountResponse(account.AccountId, account.Username, account.RoleId, account.DepartmentId, account.Status);
    }

    public async Task<AccountResponse> CreateAccountAsync(CreateAccountRequest request, int createdByAccountId, CancellationToken cancellationToken = default)
    {
        var account = new Account
        {
            Username = request.Username,
            PasswordHash = request.Password, // Mock hashing
            RoleId = request.RoleId,
            DepartmentId = request.DepartmentId,
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            CreatedByAccountId = createdByAccountId
        };

        await _unitOfWork.AccountRepository.AddAsync(account, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new AccountResponse(account.AccountId, account.Username, account.RoleId, account.DepartmentId, account.Status);
    }

    public async Task UpdateAccountStatusAsync(int accountId, string status, int updatedByAccountId, CancellationToken cancellationToken = default)
    {
        var account = await _unitOfWork.AccountRepository.GetByIdAsync(accountId, cancellationToken)
            ?? throw new KeyNotFoundException($"Account {accountId} not found.");

        account.Status = status;
        account.UpdatedAt = DateTime.UtcNow;
        account.UpdatedByAccountId = updatedByAccountId;

        _unitOfWork.AccountRepository.Update(account);
        await _unitOfWork.SaveAsync(cancellationToken);
    }

    public async Task DeleteAccountAsync(int accountId, CancellationToken cancellationToken = default)
    {
        var account = await _unitOfWork.AccountRepository.GetByIdAsync(accountId, cancellationToken)
            ?? throw new KeyNotFoundException($"Account {accountId} not found.");

        _unitOfWork.AccountRepository.Delete(account);
        await _unitOfWork.SaveAsync(cancellationToken);
    }
}
