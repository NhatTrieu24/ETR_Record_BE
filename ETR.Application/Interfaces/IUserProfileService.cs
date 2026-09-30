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
    Task<UserProfileResponse> UpdatePilotCredentialsAsync(int accountId, UpdatePilotCredentialsRequest request, int updatedByAccountId, bool isSelfUpdate, CancellationToken cancellationToken = default);
    Task<UserProfileResponse> VerifyPilotCredentialsAsync(int accountId, VerifyPilotCredentialsRequest request, int verifiedByAccountId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CredentialAttachmentDto>> GetCredentialAttachmentsAsync(int accountId, int currentAccountId, string roleName, CancellationToken cancellationToken = default);
    Task<CredentialAttachmentDto> UploadCredentialAttachmentAsync(int accountId, UploadCredentialAttachmentRequest request, int uploadedByAccountId, string roleName, CancellationToken cancellationToken = default);
    Task DeleteCredentialAttachmentAsync(int attachmentId, int currentAccountId, string roleName, CancellationToken cancellationToken = default);
}
