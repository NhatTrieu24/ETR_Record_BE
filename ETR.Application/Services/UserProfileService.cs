using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;

namespace ETR.Application.Services;

public class UserProfileService : IUserProfileService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UserProfileService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    private async Task<HashSet<int>> GetInstructorStudentIdsAsync(int instructorAccountId, CancellationToken cancellationToken)
    {
        var instructorClassIds = _unitOfWork.ClassSubjectRepository.GetQueryable()
            .Where(cs => cs.InstructorAccountId == instructorAccountId)
            .Select(cs => cs.ClassId)
            .ToHashSet();
        
        var enrollments = await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(cancellationToken);
        return enrollments.Where(e => instructorClassIds.Contains(e.ClassId)).Select(e => e.AccountId).ToHashSet();
    }

    public async Task<IEnumerable<UserProfileResponse>> GetAllProfilesAsync(CancellationToken cancellationToken = default)
    {
        var profiles = await _unitOfWork.UserProfileRepository.GetAllAsync(cancellationToken);
        
        if (_currentUserService.RoleName == "Instructor" && _currentUserService.AccountId.HasValue)
        {
            var studentIds = await GetInstructorStudentIdsAsync(_currentUserService.AccountId.Value, cancellationToken);
            profiles = profiles.Where(p => studentIds.Contains(p.AccountId)).ToList();
        }

        return profiles.Select(MapToResponse);
    }

    public async Task<IEnumerable<UserProfileResponse>> GetLearnerProfilesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _unitOfWork.RoleRepository.GetAllAsync(cancellationToken);
        var studentRole = roles.FirstOrDefault(r => r.RoleName == "Student");
        if (studentRole == null) return Enumerable.Empty<UserProfileResponse>();

        var accounts = await _unitOfWork.AccountRepository.GetAllAsync(cancellationToken);
        var studentAccountIds = accounts.Where(a => a.RoleId == studentRole.RoleId).Select(a => a.AccountId).ToHashSet();

        var profiles = await _unitOfWork.UserProfileRepository.GetAllAsync(cancellationToken);
        var learnerProfiles = profiles.Where(p => studentAccountIds.Contains(p.AccountId));

        if (_currentUserService.RoleName == "Instructor" && _currentUserService.AccountId.HasValue)
        {
            var myStudentIds = await GetInstructorStudentIdsAsync(_currentUserService.AccountId.Value, cancellationToken);
            learnerProfiles = learnerProfiles.Where(p => myStudentIds.Contains(p.AccountId));
        }

        return learnerProfiles.Select(MapToResponse);
    }

    public async Task<UserProfileResponse> GetProfileByAccountIdAsync(int accountId, CancellationToken cancellationToken = default)
    {
        var profiles = await _unitOfWork.UserProfileRepository.GetAllAsync(cancellationToken);
        var profile = profiles.FirstOrDefault(p => p.AccountId == accountId)
            ?? throw new KeyNotFoundException($"UserProfile for Account {accountId} not found.");
            
        if (_currentUserService.RoleName == "Instructor" && _currentUserService.AccountId.HasValue)
        {
            var myStudentIds = await GetInstructorStudentIdsAsync(_currentUserService.AccountId.Value, cancellationToken);
            if (!myStudentIds.Contains(accountId))
            {
                throw new KeyNotFoundException($"UserProfile for Account {accountId} not found."); // Masking 403 as 404
            }
        }
            
        return MapToResponse(profile);
    }

    public async Task<UserProfileResponse> CreateProfileAsync(CreateUserProfileRequest request, int accountId, int createdByAccountId, CancellationToken cancellationToken = default)
    {
        string? userCode = request.UserCode;
        if (string.IsNullOrWhiteSpace(userCode))
        {
            var account = await _unitOfWork.AccountRepository.GetByIdAsync(accountId, cancellationToken);
            if (account != null)
            {
                string prefix = account.RoleId switch
                {
                    1 => "ADM",
                    2 => "INS",
                    3 => "QA",
                    4 => "ACA",
                    5 => "MGR",
                    6 => "STU",
                    7 => "AUD",
                    _ => "USR"
                };

                var existingProfiles = await _unitOfWork.UserProfileRepository.GetAllAsync(cancellationToken);
                int count = existingProfiles.Count(p => p.UserCode != null && p.UserCode.StartsWith(prefix));
                userCode = $"{prefix}-{(count + 1):D3}";
            }
            else
            {
                userCode = $"USR-{DateTime.UtcNow.Ticks.ToString().Substring(10)}"; // Fallback
            }
        }

        var profile = new UserProfile
        {
            AccountId = accountId, // Link to the created account
            UserCode = userCode,
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            Organization = request.Organization,
            CreatedAt = DateTime.UtcNow,
            CreatedByAccountId = createdByAccountId
        };

        await _unitOfWork.UserProfileRepository.AddAsync(profile, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return MapToResponse(profile);
    }

    public async Task<UserProfileResponse> UpdateProfileAsync(int accountId, UpdateUserProfileRequest request, int updatedByAccountId, CancellationToken cancellationToken = default)
    {
        var profiles = await _unitOfWork.UserProfileRepository.GetAllAsync(cancellationToken);
        var profile = profiles.FirstOrDefault(p => p.AccountId == accountId)
            ?? throw new KeyNotFoundException($"UserProfile for Account {accountId} not found.");

        profile.FullName = request.FullName;
        profile.Email = request.Email;
        profile.Phone = request.Phone;
        profile.DateOfBirth = request.DateOfBirth;
        profile.Gender = request.Gender;
        profile.Organization = request.Organization;
        profile.UpdatedAt = DateTime.UtcNow;
        profile.UpdatedByAccountId = updatedByAccountId;

        _unitOfWork.UserProfileRepository.Update(profile);
        await _unitOfWork.SaveAsync(cancellationToken);

        return MapToResponse(profile);
    }

    public async Task<UserProfileResponse> UpdateProfileStatusAsync(int accountId, string status, int updatedByAccountId, CancellationToken cancellationToken = default)
    {
        var profiles = await _unitOfWork.UserProfileRepository.GetAllAsync(cancellationToken);
        var profile = profiles.FirstOrDefault(p => p.AccountId == accountId)
            ?? throw new KeyNotFoundException($"UserProfile for Account {accountId} not found.");

        var oldStatus = profile.Status;

        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            AccountId = updatedByAccountId,
            ActionType = "UPDATE",
            EntityName = nameof(UserProfile),
            RecordId = profile.AccountId,
            OldValue = oldStatus,
            NewValue = status,
            Description = $"UserProfile for Account #{accountId} status changed from '{oldStatus}' to '{status}'"
        }, cancellationToken);

        profile.Status = status;
        profile.UpdatedAt = DateTime.UtcNow;
        profile.UpdatedByAccountId = updatedByAccountId;

        _unitOfWork.UserProfileRepository.Update(profile);
        await _unitOfWork.SaveAsync(cancellationToken);

        return MapToResponse(profile);
    }

    private static UserProfileResponse MapToResponse(UserProfile p)
    {
        return new UserProfileResponse(
            p.AccountId,
            p.UserCode,
            p.FullName,
            p.Email,
            p.Phone,
            p.DateOfBirth,
            p.Gender,
            p.Organization,
            p.Status);
    }
}
