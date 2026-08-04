using ETR.Application.Compliance;
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
        return accounts.Select(a => new AccountResponse(a.AccountId, a.Username, a.RoleId, a.DepartmentId, a.Status, a.IsActive));
    }

    public async Task<AccountResponse> GetAccountByIdAsync(int accountId, CancellationToken cancellationToken = default)
    {
        var account = await _unitOfWork.AccountRepository.GetByIdAsync(accountId, cancellationToken)
            ?? throw new KeyNotFoundException($"Account {accountId} not found.");
            
        return new AccountResponse(account.AccountId, account.Username, account.RoleId, account.DepartmentId, account.Status, account.IsActive);
    }

    public async Task<AccountResponse> CreateAccountAsync(CreateAccountRequest request, int createdByAccountId, bool isCallerAdmin, CancellationToken cancellationToken = default)
    {
        if (!isCallerAdmin)
        {
            var targetRole = await _unitOfWork.RoleRepository.GetByIdAsync(request.RoleId, cancellationToken);
            if (targetRole == null || targetRole.RoleName != "Student")
            {
                throw new UnauthorizedAccessException("Academic staff can only create Student accounts.");
            }
        }
        var account = new Account
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            RoleId = request.RoleId,
            DepartmentId = request.DepartmentId,
            Status = "Active",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedByAccountId = createdByAccountId
        };

        await _unitOfWork.AccountRepository.AddAsync(account, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new AccountResponse(account.AccountId, account.Username, account.RoleId, account.DepartmentId, account.Status, account.IsActive);
    }

    public async Task UpdateAccountStatusAsync(int accountId, string status, int updatedByAccountId, CancellationToken cancellationToken = default)
    {
        var account = await _unitOfWork.AccountRepository.GetByIdAsync(accountId, cancellationToken)
            ?? throw new KeyNotFoundException($"Account {accountId} not found.");

        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            AccountId = updatedByAccountId,
            ActionType = "UPDATE",
            EntityName = nameof(Account),
            RecordId = accountId,
            OldValue = account.Status,
            NewValue = status,
            Description = $"Account #{accountId} status changed from '{account.Status}' to '{status}'"
        }, cancellationToken);

        account.Status = status;
        account.UpdatedAt = DateTime.UtcNow;
        account.UpdatedByAccountId = updatedByAccountId;

        _unitOfWork.AccountRepository.Update(account);
        await _unitOfWork.SaveAsync(cancellationToken);
    }

    public async Task UpdateAccountRoleAsync(int accountId, int roleId, int updatedByAccountId, CancellationToken cancellationToken = default)
    {
        var account = await _unitOfWork.AccountRepository.GetByIdAsync(accountId, cancellationToken)
            ?? throw new KeyNotFoundException($"Account {accountId} not found.");

        // Role assignment is the most compliance-sensitive account action (grants system access) —
        // the FRD explicitly calls it out as the one most requiring an audit trail.
        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            AccountId = updatedByAccountId,
            ActionType = "UPDATE",
            EntityName = nameof(Account),
            RecordId = accountId,
            OldValue = account.RoleId.ToString(),
            NewValue = roleId.ToString(),
            Description = $"Account #{accountId} role changed from RoleId {account.RoleId} to {roleId}"
        }, cancellationToken);

        account.RoleId = roleId;
        account.UpdatedAt = DateTime.UtcNow;
        account.UpdatedByAccountId = updatedByAccountId;

        _unitOfWork.AccountRepository.Update(account);
        await _unitOfWork.SaveAsync(cancellationToken);
    }

    public async Task UpdateAccountDepartmentAsync(int accountId, int departmentId, int updatedByAccountId, CancellationToken cancellationToken = default)
    {
        var account = await _unitOfWork.AccountRepository.GetByIdAsync(accountId, cancellationToken)
            ?? throw new KeyNotFoundException($"Account {accountId} not found.");

        _ = await _unitOfWork.DepartmentRepository.GetByIdAsync(departmentId, cancellationToken)
            ?? throw new BusinessRuleViolationException($"Department {departmentId} does not exist.");

        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            AccountId = updatedByAccountId,
            ActionType = "UPDATE",
            EntityName = nameof(Account),
            RecordId = accountId,
            OldValue = account.DepartmentId.ToString(),
            NewValue = departmentId.ToString(),
            Description = $"Account #{accountId} department changed from DepartmentId {account.DepartmentId} to {departmentId}"
        }, cancellationToken);

        account.DepartmentId = departmentId;
        account.UpdatedAt = DateTime.UtcNow;
        account.UpdatedByAccountId = updatedByAccountId;

        _unitOfWork.AccountRepository.Update(account);
        await _unitOfWork.SaveAsync(cancellationToken);
    }

    public async Task DeleteAccountAsync(int accountId, int deletedByAccountId, CancellationToken cancellationToken = default)
    {
        var account = await _unitOfWork.AccountRepository.GetByIdAsync(accountId, cancellationToken)
            ?? throw new KeyNotFoundException($"Account {accountId} not found.");

        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            AccountId = deletedByAccountId,
            ActionType = "DELETE",
            EntityName = nameof(Account),
            RecordId = accountId,
            OldValue = account.Status,
            NewValue = "Inactive",
            Description = $"Account #{accountId} deactivated (soft delete)"
        }, cancellationToken);

        account.IsActive = false;
        account.Status = "Inactive";
        account.UpdatedAt = DateTime.UtcNow;
        account.UpdatedByAccountId = deletedByAccountId;

        _unitOfWork.AccountRepository.Update(account);
        await _unitOfWork.SaveAsync(cancellationToken);
    }
}
