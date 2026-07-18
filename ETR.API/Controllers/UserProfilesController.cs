using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// [Module/Flow]: Identity &amp; Access Management
/// [Core Responsibility]: Manages demographic user profile data and handles user-specific views.
/// [Target Audience]: All Roles (for own profile), Admin (for all profiles)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserProfilesController : ControllerBase
{
    private readonly IUserProfileService _userProfileService;
    private readonly ICurrentUserService _currentUserService;

    public UserProfilesController(IUserProfileService userProfileService, ICurrentUserService currentUserService)
    {
        _userProfileService = userProfileService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<UserProfileResponse>>> GetAllProfiles(CancellationToken cancellationToken)
    {
        var profiles = await _userProfileService.GetAllProfilesAsync(cancellationToken);
        return Ok(profiles);
    }

    [HttpGet("learners")]
    public async Task<ActionResult<IEnumerable<UserProfileResponse>>> GetLearnerProfiles(CancellationToken cancellationToken)
    {
        var profiles = await _userProfileService.GetLearnerProfilesAsync(cancellationToken);
        return Ok(profiles);
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserProfileResponse>> GetMyProfile(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var profile = await _userProfileService.GetProfileByAccountIdAsync(userId, cancellationToken);
        return Ok(profile);
    }

    [HttpGet("{accountId:int}")]
    public async Task<ActionResult<UserProfileResponse>> GetProfileByAccountId(int accountId, CancellationToken cancellationToken)
    {
        var profile = await _userProfileService.GetProfileByAccountIdAsync(accountId, cancellationToken);
        return Ok(profile);
    }

    [HttpPost("{accountId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserProfileResponse>> CreateProfile(int accountId, [FromBody] CreateUserProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var profile = await _userProfileService.CreateProfileAsync(request, accountId, userId, cancellationToken);
        return CreatedAtAction(nameof(GetProfileByAccountId), new { accountId = profile.AccountId }, profile);
    }

    [HttpPut("me")]
    public async Task<ActionResult<UserProfileResponse>> UpdateMyProfile([FromBody] UpdateUserProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var profile = await _userProfileService.UpdateProfileAsync(userId, request, userId, cancellationToken);
        return Ok(profile);
    }

    [HttpPut("{accountId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserProfileResponse>> UpdateProfile(int accountId, [FromBody] UpdateUserProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var profile = await _userProfileService.UpdateProfileAsync(accountId, request, userId, cancellationToken);
        return Ok(profile);
    }
}
