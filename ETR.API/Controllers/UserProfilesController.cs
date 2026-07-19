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

    /// <summary>
    /// [Module/Flow]: Identity &amp; Access Management
    /// [Core Responsibility]: Retrieves all user profiles.
    /// [Target Audience]: Admin
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<UserProfileResponse>>> GetAllProfiles(CancellationToken cancellationToken)
    {
        var profiles = await _userProfileService.GetAllProfilesAsync(cancellationToken);
        return Ok(profiles);
    }

    /// <summary>
    /// [Module/Flow]: Identity &amp; Access Management
    /// [Core Responsibility]: Retrieves all learner profiles.
    /// [Target Audience]: Admin, Academic, TrainingManager
    /// </summary>
    [HttpGet("learners")]
    public async Task<ActionResult<IEnumerable<UserProfileResponse>>> GetLearnerProfiles(CancellationToken cancellationToken)
    {
        var profiles = await _userProfileService.GetLearnerProfilesAsync(cancellationToken);
        return Ok(profiles);
    }

    /// <summary>
    /// [Module/Flow]: Identity &amp; Access Management
    /// [Core Responsibility]: Retrieves the profile of the currently authenticated user.
    /// [Target Audience]: All Roles
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileResponse>> GetMyProfile(CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var profile = await _userProfileService.GetProfileByAccountIdAsync(accountId, cancellationToken);
        return Ok(profile);
    }

    /// <summary>
    /// [Module/Flow]: Identity &amp; Access Management
    /// [Core Responsibility]: Retrieves a user profile by account ID.
    /// [Target Audience]: Admin
    /// </summary>
    [HttpGet("{accountId:int}")]
    public async Task<ActionResult<UserProfileResponse>> GetProfileByAccountId(int accountId, CancellationToken cancellationToken)
    {
        var profile = await _userProfileService.GetProfileByAccountIdAsync(accountId, cancellationToken);
        return Ok(profile);
    }

    /// <summary>
    /// [Module/Flow]: Identity &amp; Access Management
    /// [Core Responsibility]: Creates a new user profile for an account.
    /// [Target Audience]: Admin
    /// </summary>
    [HttpPost("{accountId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserProfileResponse>> CreateProfile(int accountId, [FromBody] CreateUserProfileRequest request, CancellationToken cancellationToken)
    {
        var currentAccountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var profile = await _userProfileService.CreateProfileAsync(request, accountId, currentAccountId, cancellationToken);
        return CreatedAtAction(nameof(GetProfileByAccountId), new { accountId = profile.AccountId }, profile);
    }

    /// <summary>
    /// [Module/Flow]: Identity &amp; Access Management
    /// [Core Responsibility]: Updates the profile of the currently authenticated user.
    /// [Target Audience]: All Roles
    /// </summary>
    [HttpPut("me")]
    public async Task<ActionResult<UserProfileResponse>> UpdateMyProfile([FromBody] UpdateUserProfileRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var profile = await _userProfileService.UpdateProfileAsync(accountId, request, accountId, cancellationToken);
        return Ok(profile);
    }

    /// <summary>
    /// [Module/Flow]: Identity &amp; Access Management
    /// [Core Responsibility]: Updates a specific user profile by account ID.
    /// [Target Audience]: Admin
    /// </summary>
    [HttpPut("{accountId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserProfileResponse>> UpdateProfile(int accountId, [FromBody] UpdateUserProfileRequest request, CancellationToken cancellationToken)
    {
        var currentAccountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var profile = await _userProfileService.UpdateProfileAsync(accountId, request, currentAccountId, cancellationToken);
        return Ok(profile);
    }
}
