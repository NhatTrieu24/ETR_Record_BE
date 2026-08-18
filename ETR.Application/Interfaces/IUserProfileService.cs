using ETR.Application.DTOs;
using ETR.Domain.Enums;

namespace ETR.Application.Interfaces;

public interface IUserProfileService
{
    Task<IEnumerable<UserProfileResponse>> GetAllProfilesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<UserProfileResponse>> GetLearnerProfilesAsync(CancellationToken cancellationToken = default);
    Task<UserProfileResponse> GetProfileByAccountIdAsync(int accountId, CancellationToken cancellationToken = default);
    Task<UserProfileResponse> CreateProfileAsync(CreateUserProfileRequest request, int accountId, int createdByAccountId, CancellationToken cancellationToken = default);
    Task<UserProfileResponse> UpdateProfileAsync(int accountId, UpdateUserProfileRequest request, int updatedByAccountId, CancellationToken cancellationToken = default);
    Task<UserProfileResponse> UpdateProfileStatusAsync(int accountId, LearnerStatus status, int updatedByAccountId, CancellationToken cancellationToken = default);
}
