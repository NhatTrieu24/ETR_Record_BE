using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// [Module/Flow]: Quản lý Định danh &amp; Truy cập
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
    /// [Module/Flow]: Quản lý Định danh &amp; Truy cập
    /// [Core Responsibility]: Lấy danh sách tất cả các hồ sơ người dùng (user profiles).
    /// [Target Audience]: Admin, Academic
    /// </summary>
    // QA/Audit need full visibility for verification/audit purposes. Instructor is granted here per
    // FE report B4 (report_20260803.md) despite the over-provisioning risk of full enumeration —
    // scoping Instructor to "students in classes they teach" is tracked separately as H20 in
    // LO_TRINH_HOAN_THIEN_DU_AN.md and should replace this blanket grant once implemented.
    [HttpGet]
    [Authorize(Roles = "Admin,Academic,QA,Audit,Instructor")]
    public async Task<ActionResult<IEnumerable<UserProfileResponse>>> GetAllUserProfiles(CancellationToken cancellationToken)
    {
        var profiles = await _userProfileService.GetAllProfilesAsync(cancellationToken);
        return Ok(profiles);
    }

    /// <summary>
    /// [Module/Flow]: Quản lý Định danh &amp; Truy cập
    /// [Core Responsibility]: Lấy danh sách tất cả các hồ sơ học viên (learner profiles).
    /// [Target Audience]: Admin, Academic, TrainingManager
    /// </summary>
    [HttpGet("learners")]
    [Authorize(Roles = "Admin,Academic,TrainingManager,QA,Audit,Instructor")]
    public async Task<ActionResult<IEnumerable<UserProfileResponse>>> GetLearnerProfiles(CancellationToken cancellationToken)
    {
        var profiles = await _userProfileService.GetLearnerProfilesAsync(cancellationToken);
        return Ok(profiles);
    }

    /// <summary>
    /// [Module/Flow]: Quản lý Định danh &amp; Truy cập
    /// [Core Responsibility]: Lấy hồ sơ của người dùng hiện đang xác thực.
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
    /// [Module/Flow]: Quản lý Định danh &amp; Truy cập
    /// [Core Responsibility]: Lấy hồ sơ người dùng theo ID tài khoản.
    /// [Target Audience]: Admin, Academic
    /// </summary>
    // Single-profile-by-ID lookup is lower risk than enumerating all profiles (caller already needs
    // to know the accountId from some other legitimate join, e.g. CourseEnrollment) — Instructor
    // included here to unblock the common "look up this student's real name" enrichment case FE needs.
    [HttpGet("{accountId:int}")]
    [Authorize(Roles = "Admin,Academic,QA,Audit,Instructor")]
    public async Task<ActionResult<UserProfileResponse>> GetUserProfileByAccountId(int accountId, CancellationToken cancellationToken)
    {
        var profile = await _userProfileService.GetProfileByAccountIdAsync(accountId, cancellationToken);
        return Ok(profile);
    }

    /// <summary>
    /// [Module/Flow]: Quản lý Định danh &amp; Truy cập
    /// [Core Responsibility]: Tạo một hồ sơ người dùng mới cho một tài khoản.
    /// [Target Audience]: Admin, Academic
    /// </summary>
    [HttpPost("{accountId:int}")]
    [Authorize(Roles = "Admin,Academic")]
    public async Task<ActionResult<UserProfileResponse>> CreateUserProfile(int accountId, [FromBody] CreateUserProfileRequest request, CancellationToken cancellationToken)
    {
        var currentAccountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var profile = await _userProfileService.CreateProfileAsync(request, accountId, currentAccountId, cancellationToken);
        return CreatedAtAction(nameof(GetUserProfileByAccountId), new { accountId = profile.AccountId }, profile);
    }

    /// <summary>
    /// [Module/Flow]: Quản lý Định danh &amp; Truy cập
    /// [Core Responsibility]: Cập nhật hồ sơ của người dùng hiện đang xác thực.
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
    /// [Module/Flow]: Quản lý Định danh &amp; Truy cập
    /// [Core Responsibility]: Cập nhật một hồ sơ người dùng cụ thể theo ID tài khoản.
    /// [Target Audience]: Admin, Academic
    /// </summary>
    [HttpPut("{accountId:int}")]
    [Authorize(Roles = "Admin,Academic")]
    public async Task<ActionResult<UserProfileResponse>> UpdateUserProfile(int accountId, [FromBody] UpdateUserProfileRequest request, CancellationToken cancellationToken)
    {
        var currentAccountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var profile = await _userProfileService.UpdateProfileAsync(accountId, request, currentAccountId, cancellationToken);
        return Ok(profile);
    }

    /// <summary>
    /// [Module/Flow]: Quản lý Định danh &amp; Truy cập
    /// [Core Responsibility]: Cập nhật trạng thái tổng quát (Active/Withdrawn/Graduated) của học viên.
    /// [Target Audience]: Admin, Academic
    /// </summary>
    [HttpPut("{accountId:int}/status")]
    [Authorize(Roles = "Admin,Academic")]
    public async Task<ActionResult<UserProfileResponse>> UpdateUserProfileStatus(int accountId, [FromBody] UpdateUserProfileStatusRequest request, CancellationToken cancellationToken)
    {
        var currentAccountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var profile = await _userProfileService.UpdateProfileStatusAsync(accountId, request.Status, currentAccountId, cancellationToken);
        return Ok(profile);
    }

    /// <summary>
    /// [Module/Flow]: Quản lý Năng định Phi công (Pilot Credentials)
    /// [Core Responsibility]: Cập nhật thông tin năng định/bằng lái của chính người dùng hiện tại (Học viên tự khai -> Chưa xác minh).
    /// [Target Audience]: All Roles (Student, etc.)
    /// </summary>
    [HttpPut("me/credentials")]
    public async Task<ActionResult<UserProfileResponse>> UpdateMyCredentials([FromBody] UpdatePilotCredentialsRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var profile = await _userProfileService.UpdatePilotCredentialsAsync(accountId, request, accountId, isSelfUpdate: true, cancellationToken);
        return Ok(profile);
    }

    /// <summary>
    /// [Module/Flow]: Quản lý Năng định Phi công (Pilot Credentials)
    /// [Core Responsibility]: Cập nhật thông tin bằng lái/năng định cho một tài khoản cụ thể.
    /// [Target Audience]: Admin, Academic
    /// </summary>
    [HttpPut("{accountId:int}/credentials")]
    [Authorize(Roles = "Admin,Academic")]
    public async Task<ActionResult<UserProfileResponse>> UpdateUserCredentials(int accountId, [FromBody] UpdatePilotCredentialsRequest request, CancellationToken cancellationToken)
    {
        var currentAccountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var profile = await _userProfileService.UpdatePilotCredentialsAsync(accountId, request, currentAccountId, isSelfUpdate: false, cancellationToken);
        return Ok(profile);
    }

    /// <summary>
    /// [Module/Flow]: Quản lý Năng định Phi công (Pilot Credentials)
    /// [Core Responsibility]: Xác minh (Verify) hoặc hủy xác minh thông tin năng định/bằng lái của học viên.
    /// [Target Audience]: Admin, Academic
    /// </summary>
    [HttpPut("{accountId:int}/verify-credentials")]
    [Authorize(Roles = "Admin,Academic")]
    public async Task<ActionResult<UserProfileResponse>> VerifyUserCredentials(int accountId, [FromBody] VerifyPilotCredentialsRequest request, CancellationToken cancellationToken)
    {
        var currentAccountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var profile = await _userProfileService.VerifyPilotCredentialsAsync(accountId, request, currentAccountId, cancellationToken);
        return Ok(profile);
    }

    /// <summary>
    /// [Module/Flow]: Quản lý Minh chứng Năng định (Credential Evidences)
    /// [Core Responsibility]: Lấy danh sách tệp đính kèm/minh chứng năng định (bằng lái, y tế, ELP) của tài khoản.
    /// [Target Audience]: Student (xem của mình), Admin, Academic, QA, Audit
    /// </summary>
    [HttpGet("{accountId:int}/attachments")]
    public async Task<ActionResult<IEnumerable<CredentialAttachmentDto>>> GetCredentialAttachments(int accountId, CancellationToken cancellationToken)
    {
        var currentAccountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var attachments = await _userProfileService.GetCredentialAttachmentsAsync(accountId, currentAccountId, _currentUserService.RoleName ?? string.Empty, cancellationToken);
        return Ok(attachments);
    }

    /// <summary>
    /// [Module/Flow]: Quản lý Minh chứng Năng định (Credential Evidences)
    /// [Core Responsibility]: Đăng ký tải lên tệp minh chứng năng định (bằng lái, y tế, ELP) cho tài khoản.
    /// [Target Audience]: Student (tải lên của mình), Admin, Academic
    /// </summary>
    [HttpPost("{accountId:int}/attachments")]
    public async Task<ActionResult<CredentialAttachmentDto>> UploadCredentialAttachment(int accountId, [FromBody] UploadCredentialAttachmentRequest request, CancellationToken cancellationToken)
    {
        var currentAccountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var attachment = await _userProfileService.UploadCredentialAttachmentAsync(accountId, request, currentAccountId, _currentUserService.RoleName ?? string.Empty, cancellationToken);
        return Ok(attachment);
    }

    /// <summary>
    /// [Module/Flow]: Quản lý Minh chứng Năng định (Credential Evidences)
    /// [Core Responsibility]: Xóa tệp minh chứng năng định của tài khoản.
    /// [Target Audience]: Student (xóa của mình), Admin, Academic
    /// </summary>
    [HttpDelete("{accountId:int}/attachments/{attachmentId:int}")]
    public async Task<IActionResult> DeleteCredentialAttachment(int accountId, int attachmentId, CancellationToken cancellationToken)
    {
        var currentAccountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        await _userProfileService.DeleteCredentialAttachmentAsync(attachmentId, currentAccountId, _currentUserService.RoleName ?? string.Empty, cancellationToken);
        return NoContent();
    }
}

