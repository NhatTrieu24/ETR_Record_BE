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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AccountResponse>>> GetAllAccounts(CancellationToken cancellationToken)
    {
        var accounts = await _accountService.GetAllAccountsAsync(cancellationToken);
        return Ok(accounts);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AccountResponse>> GetAccountById(int id, CancellationToken cancellationToken)
    {
        var account = await _accountService.GetAccountByIdAsync(id, cancellationToken);
        return Ok(account);
    }

    [HttpPost]
    public async Task<ActionResult<AccountResponse>> CreateAccount([FromBody] CreateAccountRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var account = await _accountService.CreateAccountAsync(request, userId, cancellationToken);
        return CreatedAtAction(nameof(GetAccountById), new { id = account.AccountId }, account);
    }

    [HttpPut("{id:int}/status")]
    public async Task<ActionResult> UpdateAccountStatus(int id, [FromBody] UpdateAccountStatusRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        await _accountService.UpdateAccountStatusAsync(id, request.Status, userId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteAccount(int id, CancellationToken cancellationToken)
    {
        await _accountService.DeleteAccountAsync(id, cancellationToken);
        return NoContent();
    }
}
