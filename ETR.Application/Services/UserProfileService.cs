using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;

namespace ETR.Application.Services;

public class UserProfileService : IUserProfileService
{
    private readonly IUnitOfWork _unitOfWork;

    public UserProfileService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<UserProfileResponse>> GetAllProfilesAsync(CancellationToken cancellationToken = default)
    {
        var profiles = await _unitOfWork.UserProfileRepository.GetAllAsync(cancellationToken);
        return profiles.Select(MapToResponse);
    }

    public async Task<IEnumerable<UserProfileResponse>> GetLearnerProfilesAsync(CancellationToken cancellationToken = default)
    {
        var profiles = await _unitOfWork.UserProfileRepository.GetAllAsync(cancellationToken);
        return profiles.Where(p => p.LearnerTypeId.HasValue).Select(MapToResponse);
    }

    public async Task<UserProfileResponse> GetProfileByAccountIdAsync(int accountId, CancellationToken cancellationToken = default)
    {
        var profiles = await _unitOfWork.UserProfileRepository.GetAllAsync(cancellationToken);
        var profile = profiles.FirstOrDefault(p => p.AccountId == accountId)
            ?? throw new KeyNotFoundException($"UserProfile for Account {accountId} not found.");
            
        return MapToResponse(profile);
    }

    public async Task<UserProfileResponse> CreateProfileAsync(CreateUserProfileRequest request, int accountId, int createdByAccountId, CancellationToken cancellationToken = default)
    {
        var profile = new UserProfile
        {
            AccountId = accountId, // Link to the created account
            UserCode = request.UserCode,
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            Organization = request.Organization,
            LearnerTypeId = request.LearnerTypeId,
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
        profile.LearnerTypeId = request.LearnerTypeId;
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
            p.LearnerTypeId);
    }
}
