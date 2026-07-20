using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// [Module/Flow]: Quản lý Định danh &amp; Truy cập
/// [Core Responsibility]: Manages core system authentication accounts, roles, departments, and statuses.
/// [Target Audience]: Admin
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly ICurrentUserService _currentUserService;

    public AccountsController(IAccountService accountService, ICurrentUserService currentUserService)
    {
        _accountService = accountService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// [Module/Flow]: Quản lý Định danh &amp; Truy cập
    /// [Core Responsibility]: Lấy danh sách tất cả các tài khoản.
    /// [Target Audience]: Admin
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AccountResponse>>> GetAllAccounts(CancellationToken cancellationToken)
    {
        var accounts = await _accountService.GetAllAccountsAsync(cancellationToken);
        return Ok(accounts);
    }

    /// <summary>
    /// [Module/Flow]: Quản lý Định danh &amp; Truy cập
    /// [Core Responsibility]: Lấy thông tin một tài khoản cụ thể theo ID.
    /// [Target Audience]: Admin
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AccountResponse>> GetAccountById(int id, CancellationToken cancellationToken)
    {
        var account = await _accountService.GetAccountByIdAsync(id, cancellationToken);
        return Ok(account);
    }

    /// <summary>
    /// [Module/Flow]: Quản lý Định danh &amp; Truy cập
    /// [Core Responsibility]: Tạo một tài khoản hệ thống mới.
    /// [Target Audience]: Admin
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AccountResponse>> CreateAccount([FromBody] CreateAccountRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var account = await _accountService.CreateAccountAsync(request, accountId, cancellationToken);
        return CreatedAtAction(nameof(GetAccountById), new { id = account.AccountId }, account);
    }

    /// <summary>
    /// [Module/Flow]: Quản lý Định danh &amp; Truy cập
    /// [Core Responsibility]: Cập nhật trạng thái của một tài khoản hiện có.
    /// [Target Audience]: Admin
    /// </summary>
    [HttpPut("{id:int}/status")]
    public async Task<ActionResult> UpdateAccountStatus(int id, [FromBody] UpdateAccountStatusRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        await _accountService.UpdateAccountStatusAsync(id, request.Status, accountId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// [Module/Flow]: Quản lý Định danh &amp; Truy cập
    /// [Core Responsibility]: Xóa một tài khoản khỏi hệ thống.
    /// [Target Audience]: Admin
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteAccount(int id, CancellationToken cancellationToken)
    {
        await _accountService.DeleteAccountAsync(id, cancellationToken);
        return NoContent();
    }
}


