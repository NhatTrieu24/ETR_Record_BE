using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// [Module/Flow]: Identity &amp; Access Management
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
    /// [Module/Flow]: Identity &amp; Access Management
    /// [Core Responsibility]: Retrieves all accounts.
    /// [Target Audience]: Admin
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AccountResponse>>> GetAllAccounts(CancellationToken cancellationToken)
    {
        var accounts = await _accountService.GetAllAccountsAsync(cancellationToken);
        return Ok(accounts);
    }

    /// <summary>
    /// [Module/Flow]: Identity &amp; Access Management
    /// [Core Responsibility]: Retrieves a specific account by ID.
    /// [Target Audience]: Admin
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AccountResponse>> GetAccountById(int id, CancellationToken cancellationToken)
    {
        var account = await _accountService.GetAccountByIdAsync(id, cancellationToken);
        return Ok(account);
    }

    /// <summary>
    /// [Module/Flow]: Identity &amp; Access Management
    /// [Core Responsibility]: Creates a new system account.
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
    /// [Module/Flow]: Identity &amp; Access Management
    /// [Core Responsibility]: Updates the status of an existing account.
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
    /// [Module/Flow]: Identity &amp; Access Management
    /// [Core Responsibility]: Deletes an account from the system.
    /// [Target Audience]: Admin
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteAccount(int id, CancellationToken cancellationToken)
    {
        await _accountService.DeleteAccountAsync(id, cancellationToken);
        return NoContent();
    }
}
