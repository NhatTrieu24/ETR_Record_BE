using ETR.Application.Compliance;
using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using ETR.Domain.Enums;

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
        var allProfiles = await _unitOfWork.UserProfileRepository.GetAllIncludingDeletedAsync(cancellationToken);
        var profiles = allProfiles.AsEnumerable();
        
        if (_currentUserService.RoleName == "Instructor" && _currentUserService.AccountId.HasValue)
        {
            var studentIds = await GetInstructorStudentIdsAsync(_currentUserService.AccountId.Value, cancellationToken);
            profiles = profiles.Where(p => studentIds.Contains(p.AccountId));
        }

        return profiles.Select(MapToResponse);
    }

    public async Task<IEnumerable<UserProfileResponse>> GetLearnerProfilesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _unitOfWork.RoleRepository.GetAllAsync(cancellationToken);
        var studentRole = roles.FirstOrDefault(r => r.RoleName == "Student");
        if (studentRole == null) return Enumerable.Empty<UserProfileResponse>();

        var accounts = await _unitOfWork.AccountRepository.GetAllIncludingDeletedAsync(cancellationToken);
        var studentAccountIds = accounts.Where(a => a.RoleId == studentRole.RoleId).Select(a => a.AccountId).ToHashSet();

        var profiles = await _unitOfWork.UserProfileRepository.GetAllIncludingDeletedAsync(cancellationToken);
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
        var profiles = await _unitOfWork.UserProfileRepository.GetAllIncludingDeletedAsync(cancellationToken);
        var profile = profiles.FirstOrDefault(p => p.AccountId == accountId)
            ?? throw new KeyNotFoundException($"UserProfile for Account {accountId} not found.");
            
        if (_currentUserService.RoleName == "Instructor" && _currentUserService.AccountId.HasValue)
        {
            bool isOwnProfile = _currentUserService.AccountId.Value == accountId;
            if (!isOwnProfile)
            {
                var myStudentIds = await GetInstructorStudentIdsAsync(_currentUserService.AccountId.Value, cancellationToken);
                if (!myStudentIds.Contains(accountId))
                {
                    throw new KeyNotFoundException($"UserProfile for Account {accountId} not found."); // Masking 403 as 404
                }
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

        if (string.IsNullOrWhiteSpace(request.FullName) || !System.Text.RegularExpressions.Regex.IsMatch(request.FullName.Trim(), @"^[\p{L}\s]+$"))
        {
            throw new BusinessRuleViolationException("Họ và tên chỉ được chứa chữ cái và khoảng trắng, không được bắt đầu bằng số hoặc chứa ký tự đặc biệt.");
        }

        if (!string.IsNullOrWhiteSpace(request.Phone) && !System.Text.RegularExpressions.Regex.IsMatch(request.Phone.Trim(), @"^0[0-9]{9,10}$"))
        {
            throw new BusinessRuleViolationException("Số điện thoại phải bắt đầu bằng số 0 và gồm 10-11 chữ số, không chứa ký tự đặc biệt.");
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(request.Email.Trim(), @"^[a-zA-Z][a-zA-Z0-9._%+-]*@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
            {
                throw new BusinessRuleViolationException("Email phải là địa chỉ email hợp lệ và bắt đầu bằng chữ cái.");
            }

            var existingProfiles = await _unitOfWork.UserProfileRepository.GetAllAsync(cancellationToken);
            if (existingProfiles.Any(p => p.Email == request.Email))
            {
                throw new BusinessRuleViolationException($"A profile with email '{request.Email}' already exists.");
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

        if (string.IsNullOrWhiteSpace(request.FullName) || !System.Text.RegularExpressions.Regex.IsMatch(request.FullName.Trim(), @"^[\p{L}\s]+$"))
        {
            throw new BusinessRuleViolationException("Họ và tên chỉ được chứa chữ cái và khoảng trắng, không được bắt đầu bằng số hoặc chứa ký tự đặc biệt.");
        }

        if (!string.IsNullOrWhiteSpace(request.Phone) && !System.Text.RegularExpressions.Regex.IsMatch(request.Phone.Trim(), @"^0[0-9]{9,10}$"))
        {
            throw new BusinessRuleViolationException("Số điện thoại phải bắt đầu bằng số 0 và gồm 10-11 chữ số, không chứa ký tự đặc biệt.");
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(request.Email.Trim(), @"^[a-zA-Z][a-zA-Z0-9._%+-]*@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
            {
                throw new BusinessRuleViolationException("Email phải là địa chỉ email hợp lệ và bắt đầu bằng chữ cái.");
            }

            if (profiles.Any(p => p.AccountId != accountId && p.Email == request.Email))
            {
                throw new BusinessRuleViolationException($"A profile with email '{request.Email}' already exists.");
            }
        }

        profile.FullName = request.FullName;
        profile.Email = request.Email;
        profile.Phone = request.Phone;
        profile.DateOfBirth = request.DateOfBirth;
        profile.Gender = request.Gender;
        profile.Organization = request.Organization;

        bool credentialsChanged =
            profile.LicenseType != request.LicenseType ||
            profile.LicenseNumber != request.LicenseNumber ||
            profile.LicenseExpiryDate != request.LicenseExpiryDate ||
            profile.MedicalClass != request.MedicalClass ||
            profile.MedicalExpiryDate != request.MedicalExpiryDate ||
            profile.IcaoElpLevel != request.IcaoElpLevel ||
            profile.IcaoElpExpiryDate != request.IcaoElpExpiryDate ||
            profile.TypeRatings != request.TypeRatings;

        if (credentialsChanged)
        {
            profile.LicenseType = request.LicenseType;
            profile.LicenseNumber = request.LicenseNumber;
            profile.LicenseExpiryDate = request.LicenseExpiryDate;
            profile.MedicalClass = request.MedicalClass;
            profile.MedicalExpiryDate = request.MedicalExpiryDate;
            profile.IcaoElpLevel = request.IcaoElpLevel;
            profile.IcaoElpExpiryDate = request.IcaoElpExpiryDate;
            profile.TypeRatings = request.TypeRatings;

            // Any substantive change to credentials invalidates previous verification
            profile.IsCredentialsVerified = false;
            profile.CredentialsVerifiedByAccountId = null;
            profile.CredentialsVerifiedAt = null;
        }

        profile.UpdatedAt = DateTime.UtcNow;
        profile.UpdatedByAccountId = updatedByAccountId;

        _unitOfWork.UserProfileRepository.Update(profile);
        await _unitOfWork.SaveAsync(cancellationToken);

        return MapToResponse(profile);
    }

    public async Task<UserProfileResponse> UpdatePilotCredentialsAsync(int accountId, UpdatePilotCredentialsRequest request, int updatedByAccountId, bool isSelfUpdate, CancellationToken cancellationToken = default)
    {
        var profiles = await _unitOfWork.UserProfileRepository.GetAllAsync(cancellationToken);
        var profile = profiles.FirstOrDefault(p => p.AccountId == accountId)
            ?? throw new KeyNotFoundException($"UserProfile for Account {accountId} not found.");

        if (request.IcaoElpLevel.HasValue && (request.IcaoElpLevel.Value < 1 || request.IcaoElpLevel.Value > 6))
        {
            throw new BusinessRuleViolationException("ICAO ELP Level phải nằm trong khoảng từ 1 đến 6.");
        }

        bool credentialsChanged =
            profile.LicenseType != request.LicenseType ||
            profile.LicenseNumber != request.LicenseNumber ||
            profile.LicenseExpiryDate != request.LicenseExpiryDate ||
            profile.MedicalClass != request.MedicalClass ||
            profile.MedicalExpiryDate != request.MedicalExpiryDate ||
            profile.IcaoElpLevel != request.IcaoElpLevel ||
            profile.IcaoElpExpiryDate != request.IcaoElpExpiryDate ||
            profile.TypeRatings != request.TypeRatings;

        var oldSummary = $"License: {profile.LicenseType}/{profile.LicenseNumber}, Medical: {profile.MedicalClass}, ELP: {profile.IcaoElpLevel}, TypeRatings: {profile.TypeRatings}";
        var newSummary = $"License: {request.LicenseType}/{request.LicenseNumber}, Medical: {request.MedicalClass}, ELP: {request.IcaoElpLevel}, TypeRatings: {request.TypeRatings}";

        if (credentialsChanged)
        {
            profile.LicenseType = request.LicenseType;
            profile.LicenseNumber = request.LicenseNumber;
            profile.LicenseExpiryDate = request.LicenseExpiryDate;
            profile.MedicalClass = request.MedicalClass;
            profile.MedicalExpiryDate = request.MedicalExpiryDate;
            profile.IcaoElpLevel = request.IcaoElpLevel;
            profile.IcaoElpExpiryDate = request.IcaoElpExpiryDate;
            profile.TypeRatings = request.TypeRatings;

            // Any substantive change invalidates previous verification regardless of who made the update
            profile.IsCredentialsVerified = false;
            profile.CredentialsVerifiedByAccountId = null;
            profile.CredentialsVerifiedAt = null;
        }

        profile.UpdatedAt = DateTime.UtcNow;
        profile.UpdatedByAccountId = updatedByAccountId;

        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            AccountId = updatedByAccountId,
            ActionType = AuditActionType.UPDATE.ToString(),
            EntityName = nameof(UserProfile),
            RecordId = profile.AccountId,
            OldValue = oldSummary,
            NewValue = newSummary,
            Description = $"Pilot credentials updated for Account #{accountId} (SelfUpdate: {isSelfUpdate}, Changed: {credentialsChanged})"
        }, cancellationToken);

        _unitOfWork.UserProfileRepository.Update(profile);
        await _unitOfWork.SaveAsync(cancellationToken);

        return MapToResponse(profile);
    }

    public async Task<UserProfileResponse> VerifyPilotCredentialsAsync(int accountId, VerifyPilotCredentialsRequest request, int verifiedByAccountId, CancellationToken cancellationToken = default)
    {
        var profiles = await _unitOfWork.UserProfileRepository.GetAllAsync(cancellationToken);
        var profile = profiles.FirstOrDefault(p => p.AccountId == accountId)
            ?? throw new KeyNotFoundException($"UserProfile for Account {accountId} not found.");

        bool oldVerified = profile.IsCredentialsVerified;
        profile.IsCredentialsVerified = request.IsVerified;
        profile.CredentialsVerifiedByAccountId = request.IsVerified ? verifiedByAccountId : null;
        profile.CredentialsVerifiedAt = request.IsVerified ? DateTime.UtcNow : null;
        profile.UpdatedAt = DateTime.UtcNow;
        profile.UpdatedByAccountId = verifiedByAccountId;

        string verificationBasis;
        if (!request.IsVerified)
        {
            verificationBasis = "Revoked / Unverified";
        }
        else
        {
            if (request.ReviewedAttachmentIds != null && request.ReviewedAttachmentIds.Count > 0)
            {
                var allAttachments = _unitOfWork.AttachmentRepository != null
                    ? await _unitOfWork.AttachmentRepository.GetAllAsync(cancellationToken)
                    : Enumerable.Empty<Attachment>();

                var attachmentMap = allAttachments.ToDictionary(a => a.AttachmentId);
                var reviewedNames = new List<string>();

                foreach (var attId in request.ReviewedAttachmentIds)
                {
                    if (!attachmentMap.TryGetValue(attId, out var att) ||
                        att.IsDeleted ||
                        att.OwnerType != nameof(UserProfile) ||
                        att.OwnerId != accountId)
                    {
                        throw new BusinessRuleViolationException(
                            $"Tài liệu minh chứng #{attId} không tồn tại, đã bị xóa hoặc không thuộc hồ sơ của học viên #{accountId}.");
                    }
                    reviewedNames.Add($"{att.DocType ?? "General"}:{att.FileName}");
                }

                verificationBasis = $"Document review verified ({string.Join(", ", reviewedNames)})";
            }
            else
            {
                if (string.IsNullOrWhiteSpace(request.VerificationMethod))
                {
                    throw new BusinessRuleViolationException(
                        "Phương thức xác minh (VerificationMethod) là bắt buộc khi xác minh ngoại tuyến hoặc không đính kèm ID minh chứng.");
                }

                verificationBasis = $"Verified via {request.VerificationMethod.Trim()} (Offline/Direct check without uploaded attachments)";
            }
        }

        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            AccountId = verifiedByAccountId,
            ActionType = AuditActionType.UPDATE.ToString(),
            EntityName = nameof(UserProfile),
            RecordId = profile.AccountId,
            OldValue = $"Verified: {oldVerified}",
            NewValue = $"Verified: {request.IsVerified}. Basis: {verificationBasis}. Comment: {request.Comment}",
            Description = $"Pilot credentials for Account #{accountId} {(request.IsVerified ? "VERIFIED" : "UNVERIFIED")} by Account #{verifiedByAccountId}. Basis: {verificationBasis}"
        }, cancellationToken);

        _unitOfWork.UserProfileRepository.Update(profile);
        await _unitOfWork.SaveAsync(cancellationToken);

        return MapToResponse(profile);
    }

    public async Task<IEnumerable<CredentialAttachmentDto>> GetCredentialAttachmentsAsync(int accountId, int currentAccountId, string roleName, CancellationToken cancellationToken = default)
    {
        bool isPrivilegedRole = roleName is "Admin" or "Academic" or "QA" or "Audit";
        if (!isPrivilegedRole && accountId != currentAccountId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền xem tài liệu năng định / y tế của học viên này.");
        }

        var allAttachments = await _unitOfWork.AttachmentRepository.GetAllAsync(cancellationToken);
        var userAttachments = allAttachments
            .Where(a => a.OwnerType == nameof(UserProfile) && a.OwnerId == accountId && !a.IsDeleted)
            .OrderByDescending(a => a.UploadedAt)
            .Select(a => new CredentialAttachmentDto(
                a.AttachmentId,
                a.OwnerId,
                a.DocType ?? "General",
                a.FileName,
                a.Url,
                a.MimeType,
                a.FileSize,
                a.UploadedAt,
                a.UploadedByAccountId))
            .ToList();

        return userAttachments;
    }

    public async Task<CredentialAttachmentDto> UploadCredentialAttachmentAsync(int accountId, UploadCredentialAttachmentRequest request, int uploadedByAccountId, string roleName, CancellationToken cancellationToken = default)
    {
        bool isPrivilegedRole = roleName is "Admin" or "Academic";
        if (!isPrivilegedRole && accountId != uploadedByAccountId)
        {
            throw new UnauthorizedAccessException("Bạn chỉ được tải lên tài liệu minh chứng cho hồ sơ của chính mình.");
        }

        var attachment = new Attachment
        {
            OwnerType = nameof(UserProfile),
            OwnerId = accountId,
            DocType = request.DocType,
            Url = request.Url,
            FileName = request.FileName,
            PublicId = request.PublicId,
            MimeType = request.MimeType,
            FileSize = request.FileSize,
            UploadedByAccountId = uploadedByAccountId,
            UploadedAt = DateTime.UtcNow
        };

        await _unitOfWork.AttachmentRepository.AddAsync(attachment, cancellationToken);

        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            AccountId = uploadedByAccountId,
            ActionType = AuditActionType.INSERT.ToString(),
            EntityName = nameof(Attachment),
            RecordId = accountId,
            NewValue = $"DocType: {request.DocType}, File: {request.FileName}",
            Description = $"Uploaded credential document '{request.DocType}' for UserProfile Account #{accountId}"
        }, cancellationToken);

        await _unitOfWork.SaveAsync(cancellationToken);

        return new CredentialAttachmentDto(
            attachment.AttachmentId,
            attachment.OwnerId,
            attachment.DocType ?? "General",
            attachment.FileName,
            attachment.Url,
            attachment.MimeType,
            attachment.FileSize,
            attachment.UploadedAt,
            attachment.UploadedByAccountId);
    }

    public async Task DeleteCredentialAttachmentAsync(int attachmentId, int currentAccountId, string roleName, CancellationToken cancellationToken = default)
    {
        var attachment = await _unitOfWork.AttachmentRepository.GetByIdAsync(attachmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Attachment not found.");

        if (attachment.OwnerType != nameof(UserProfile))
        {
            throw new BusinessRuleViolationException("Tệp tin không thuộc hồ sơ năng định.");
        }

        bool isPrivilegedRole = roleName is "Admin" or "Academic";
        if (!isPrivilegedRole && attachment.UploadedByAccountId != currentAccountId && attachment.OwnerId != currentAccountId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền xóa tệp minh chứng này.");
        }

        attachment.IsDeleted = true;
        attachment.DeletedAt = DateTime.UtcNow;
        attachment.UpdatedAt = DateTime.UtcNow;
        attachment.UpdatedByAccountId = currentAccountId;

        _unitOfWork.AttachmentRepository.Update(attachment);

        // If the profile had verified credentials, deleting an evidence attachment invalidates the verification
        var profiles = await _unitOfWork.UserProfileRepository.GetAllAsync(cancellationToken);
        var profile = profiles.FirstOrDefault(p => p.AccountId == attachment.OwnerId);
        if (profile != null && profile.IsCredentialsVerified)
        {
            profile.IsCredentialsVerified = false;
            profile.CredentialsVerifiedByAccountId = null;
            profile.CredentialsVerifiedAt = null;
            profile.UpdatedAt = DateTime.UtcNow;
            profile.UpdatedByAccountId = currentAccountId;

            _unitOfWork.UserProfileRepository.Update(profile);

            await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
            {
                AccountId = currentAccountId,
                ActionType = AuditActionType.UPDATE.ToString(),
                EntityName = nameof(UserProfile),
                RecordId = profile.AccountId,
                OldValue = "Verified: True",
                NewValue = "Verified: False (Revoked due to evidence attachment deletion)",
                Description = $"Credential verification for Account #{profile.AccountId} was REVOKED due to deletion of evidence attachment #{attachment.AttachmentId} ({attachment.FileName})"
            }, cancellationToken);
        }

        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            AccountId = currentAccountId,
            ActionType = AuditActionType.DELETE.ToString(),
            EntityName = nameof(Attachment),
            RecordId = attachment.AttachmentId,
            Description = $"Deleted credential document #{attachment.AttachmentId} ({attachment.FileName}) for Account #{attachment.OwnerId}"
        }, cancellationToken);

        await _unitOfWork.SaveAsync(cancellationToken);
    }

    public async Task<UserProfileResponse> UpdateProfileStatusAsync(int accountId, LearnerStatus status, int updatedByAccountId, CancellationToken cancellationToken = default)
    {
        // Grounded is set/cleared only by CertificateValidityCalculator consumers (auto-detection of
        // expired certificates) — never by a manual profile edit, see LearnerStatus enum docs.
        if (status == LearnerStatus.Grounded)
        {
            throw new BusinessRuleViolationException("Status must be one of: Active, Withdrawn, Graduated. Grounded is set automatically by the system.");
        }

        var profiles = await _unitOfWork.UserProfileRepository.GetAllAsync(cancellationToken);
        var profile = profiles.FirstOrDefault(p => p.AccountId == accountId)
            ?? throw new KeyNotFoundException($"UserProfile for Account {accountId} not found.");

        var oldStatus = profile.Status;

        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            AccountId = updatedByAccountId,
            ActionType = AuditActionType.UPDATE.ToString(),
            EntityName = nameof(UserProfile),
            RecordId = profile.AccountId,
            OldValue = oldStatus.ToString(),
            NewValue = status.ToString(),
            Description = $"UserProfile for Account #{accountId} status changed from '{oldStatus}' to '{status}'"
        }, cancellationToken);

        profile.Status = status;
        profile.UpdatedAt = DateTime.UtcNow;
        profile.UpdatedByAccountId = updatedByAccountId;

        _unitOfWork.UserProfileRepository.Update(profile);
        await _unitOfWork.SaveAsync(cancellationToken);

        return MapToResponse(profile);
    }

    private UserProfileResponse MapToResponse(UserProfile p)
    {
        bool isInstructor = _currentUserService.RoleName == "Instructor";
        bool isOwnProfile = _currentUserService.AccountId.HasValue && _currentUserService.AccountId.Value == p.AccountId;

        // If Instructor viewing another student's profile, mask sensitive pilot credentials
        if (isInstructor && !isOwnProfile)
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
                p.Status,
                LicenseType: p.LicenseType,
                LicenseNumber: null,
                LicenseExpiryDate: null,
                MedicalClass: null,
                MedicalExpiryDate: null,
                IcaoElpLevel: null,
                IcaoElpExpiryDate: null,
                TypeRatings: null,
                IsCredentialsVerified: p.IsCredentialsVerified,
                CredentialsVerifiedByAccountId: null,
                CredentialsVerifiedAt: null);
        }

        return new UserProfileResponse(
            p.AccountId,
            p.UserCode,
            p.FullName,
            p.Email,
            p.Phone,
            p.DateOfBirth,
            p.Gender,
            p.Organization,
            p.Status,
            p.LicenseType,
            p.LicenseNumber,
            p.LicenseExpiryDate,
            p.MedicalClass,
            p.MedicalExpiryDate,
            p.IcaoElpLevel,
            p.IcaoElpExpiryDate,
            p.TypeRatings,
            p.IsCredentialsVerified,
            p.CredentialsVerifiedByAccountId,
            p.CredentialsVerifiedAt);
    }
}
