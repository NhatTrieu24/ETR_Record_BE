using ETR.Application.Compliance;
using ETR.Application.DTOs.Evidence;
using ETR.Application.DTOs.Evidence.Requests;
using ETR.Domain.Entities;
using ETR.Application.Interfaces;
using ETR.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ETR.Application.Services;

public class EvidenceService : IEvidenceService
{
    // Validated against FileName/MimeType metadata handed back by the FE's Cloudinary upload — the
    // backend never sees the actual bytes, so this is a defense-in-depth check on the metadata only,
    // not a guarantee (Cloudinary's own upload preset is the real enforcement point for file content).
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf"
    };

    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/pjpeg", "image/png", "image/x-png", "image/apng", "application/x-png", "image/x-citrix-png", "image/gif", "image/webp", "application/pdf"
    };

    private static readonly Dictionary<string, string> ExtensionToMimeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".jpg", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".png", "image/png" },
        { ".gif", "image/gif" },
        { ".webp", "image/webp" },
        { ".pdf", "application/pdf" }
    };

    private static readonly HashSet<string> AllowedVerificationStatuses = new(StringComparer.Ordinal)
    {
        "Verified", "Rejected"
    };

    private const string OwnerType = nameof(EvidenceFile);

    private readonly IUnitOfWork _unitOfWork;
    private readonly IAssessmentResultService _assessmentResultService;

    public EvidenceService(IUnitOfWork unitOfWork, IAssessmentResultService assessmentResultService)
    {
        _unitOfWork = unitOfWork;
        _assessmentResultService = assessmentResultService;
    }

    public async Task<IEnumerable<EvidenceResponse>> GetAllEvidencesAsync(int? currentAccountId = null, string? roleName = null, CancellationToken cancellationToken = default)
    {
        var evidences = (await _unitOfWork.EvidenceFileRepository.GetAllAsync(cancellationToken)).ToList();

        // Student chỉ xem được minh chứng của chính mình
        if (roleName == "Student" && currentAccountId.HasValue)
        {
            evidences = evidences.Where(e => e.AccountId == currentAccountId.Value).ToList();
        }

        var attachments = await GetAttachmentsByOwnerIdsAsync(evidences.Select(e => e.EvidenceFileId), cancellationToken);

        // TrainingManager chỉ xem metadata (không nhận URL tệp)
        bool isMetadataOnly = roleName == "TrainingManager";

        var profiles = (await _unitOfWork.UserProfileRepository.GetAllAsync(cancellationToken)).ToList();
        var profileMap = profiles.GroupBy(p => p.AccountId).ToDictionary(g => g.Key, g => g.First());
        var evidenceTypes = (await _unitOfWork.EvidenceTypeRepository.GetAllAsync(cancellationToken)).ToList();
        var evidenceTypeMap = evidenceTypes.GroupBy(et => et.EvidenceTypeId).ToDictionary(g => g.Key, g => g.First());
        var subjectResults = (await _unitOfWork.SubjectResultRepository.GetAllAsync(cancellationToken)).ToList();
        var srMap = subjectResults.GroupBy(sr => sr.SubjectResultId).ToDictionary(g => g.Key, g => g.First());
        var subjects = (await _unitOfWork.SubjectRepository.GetAllAsync(cancellationToken)).ToList();
        var subjectMap = subjects.GroupBy(s => s.SubjectId).ToDictionary(g => g.Key, g => g.First());
        var etrs = (await _unitOfWork.ETRCourseRecordRepository.GetAllAsync(cancellationToken)).ToList();
        var etrMap = etrs.GroupBy(e => e.ETRCourseRecordId).ToDictionary(g => g.Key, g => g.First());
        var enrollments = (await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(cancellationToken)).ToList();
        var enrollmentMap = enrollments.GroupBy(en => en.EnrollmentId).ToDictionary(g => g.Key, g => g.First());
        var courses = (await _unitOfWork.CourseRepository.GetAllAsync(cancellationToken)).ToList();
        var courseMap = courses.GroupBy(c => c.CourseId).ToDictionary(g => g.Key, g => g.First());
        var classes = (await _unitOfWork.ClassRepository.GetAllAsync(cancellationToken)).ToList();
        var classMap = classes.GroupBy(cl => cl.ClassId).ToDictionary(g => g.Key, g => g.First());

        return evidences.Select(e =>
        {
            var att = attachments.GetValueOrDefault(e.EvidenceFileId);
            var resp = MapToResponse(e, att);
            EnrichResponse(resp, e, profileMap, evidenceTypeMap, srMap, subjectMap, etrMap, enrollmentMap, courseMap, classMap);
            if (isMetadataOnly)
            {
                resp.FileUrl = string.Empty;
            }
            return resp;
        }).ToList();
    }

    public async Task<EvidenceResponse> GetEvidenceByIdAsync(int id, int? currentAccountId = null, string? roleName = null, CancellationToken cancellationToken = default)
    {
        var evidence = await _unitOfWork.EvidenceFileRepository.GetByIdAsync(id, cancellationToken);
        if (evidence == null)
            throw new KeyNotFoundException($"Evidence with ID {id} not found.");

        // Student chỉ được xem minh chứng của chính mình
        if (roleName == "Student" && currentAccountId.HasValue && evidence.AccountId != currentAccountId.Value)
        {
            throw new ForbiddenAccessException("Bạn chỉ được phép xem minh chứng của chính mình.");
        }

        var attachment = await GetAttachmentAsync(id, cancellationToken);
        var resp = MapToResponse(evidence, attachment);

        var profiles = (await _unitOfWork.UserProfileRepository.GetAllAsync(cancellationToken)).ToList();
        var profileMap = profiles.GroupBy(p => p.AccountId).ToDictionary(g => g.Key, g => g.First());
        var evidenceTypes = (await _unitOfWork.EvidenceTypeRepository.GetAllAsync(cancellationToken)).ToList();
        var evidenceTypeMap = evidenceTypes.GroupBy(et => et.EvidenceTypeId).ToDictionary(g => g.Key, g => g.First());
        var subjectResults = (await _unitOfWork.SubjectResultRepository.GetAllAsync(cancellationToken)).ToList();
        var srMap = subjectResults.GroupBy(sr => sr.SubjectResultId).ToDictionary(g => g.Key, g => g.First());
        var subjects = (await _unitOfWork.SubjectRepository.GetAllAsync(cancellationToken)).ToList();
        var subjectMap = subjects.GroupBy(s => s.SubjectId).ToDictionary(g => g.Key, g => g.First());
        var etrs = (await _unitOfWork.ETRCourseRecordRepository.GetAllAsync(cancellationToken)).ToList();
        var etrMap = etrs.GroupBy(e => e.ETRCourseRecordId).ToDictionary(g => g.Key, g => g.First());
        var enrollments = (await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(cancellationToken)).ToList();
        var enrollmentMap = enrollments.GroupBy(en => en.EnrollmentId).ToDictionary(g => g.Key, g => g.First());
        var courses = (await _unitOfWork.CourseRepository.GetAllAsync(cancellationToken)).ToList();
        var courseMap = courses.GroupBy(c => c.CourseId).ToDictionary(g => g.Key, g => g.First());
        var classes = (await _unitOfWork.ClassRepository.GetAllAsync(cancellationToken)).ToList();
        var classMap = classes.GroupBy(cl => cl.ClassId).ToDictionary(g => g.Key, g => g.First());

        EnrichResponse(resp, evidence, profileMap, evidenceTypeMap, srMap, subjectMap, etrMap, enrollmentMap, courseMap, classMap);

        // TrainingManager chỉ xem metadata (không nhận URL tệp)
        if (roleName == "TrainingManager")
        {
            resp.FileUrl = string.Empty;
        }

        return resp;
    }

    public async Task<EvidenceResponse> UploadEvidenceAsync(UploadEvidenceRequest request, int uploadedByAccountId, string? uploadedByRoleName, CancellationToken cancellationToken = default)
    {
        // "Sân nhà ai nấy đá" — Instructor can only upload Evidence into a class they are actually
        // assigned to (see ClassOwnershipValidator).
        var subjectResultForEvidence = await _unitOfWork.SubjectResultRepository.GetByIdAsync(request.SubjectResultId, cancellationToken);
        var etrForEvidence = subjectResultForEvidence != null
            ? await _unitOfWork.ETRCourseRecordRepository.GetByIdAsync(subjectResultForEvidence.EtrId, cancellationToken)
            : null;
        var enrollmentForEvidence = etrForEvidence != null
            ? await _unitOfWork.CourseEnrollmentRepository.GetByIdAsync(etrForEvidence.EnrollmentId, cancellationToken)
            : null;
        var classForEvidence = enrollmentForEvidence != null
            ? await _unitOfWork.ClassRepository.GetByIdAsync(enrollmentForEvidence.ClassId, cancellationToken)
            : null;

        var isAssigned = classForEvidence != null && subjectResultForEvidence != null && _unitOfWork.ClassSubjectRepository.GetQueryable()
            .Any(cs => cs.ClassId == classForEvidence.ClassId && cs.SubjectId == subjectResultForEvidence.SubjectId && cs.InstructorAccountId == uploadedByAccountId);
        ClassOwnershipValidator.EnsureInstructorOwnsSubject(uploadedByRoleName, isAssigned);

        var fileExtension = System.IO.Path.GetExtension(request.FileName);
        if (string.IsNullOrEmpty(fileExtension) || !AllowedExtensions.Contains(fileExtension))
            throw new ValidationException($"File extension '{fileExtension}' is not allowed. Allowed extensions: {string.Join(", ", AllowedExtensions)}.");

        var mimeType = request.MimeType;
        if (string.IsNullOrWhiteSpace(mimeType) && ExtensionToMimeMap.TryGetValue(fileExtension, out var inferredMime))
        {
            mimeType = inferredMime;
        }

        if (string.IsNullOrEmpty(mimeType) || !AllowedMimeTypes.Contains(mimeType))
            throw new ValidationException($"File content type '{request.MimeType}' is not allowed.");

        if (!Uri.TryCreate(request.FileUrl, UriKind.Absolute, out var parsedUrl) || parsedUrl.Scheme != Uri.UriSchemeHttps)
            throw new ValidationException("FileUrl must be an absolute https URL.");

        if (request.FileSize.HasValue && request.FileSize.Value > 10 * 1024 * 1024)
            throw new ValidationException("Dung lượng tệp vượt quá giới hạn tối đa cho phép là 10 MB.");

        // Treat 0 or negative as null for nullable FK fields to avoid FK violations
        var attendanceRecordId = request.AttendanceRecordId.HasValue && request.AttendanceRecordId.Value > 0
            ? request.AttendanceRecordId
            : null;
        var assessmentResultId = request.AssessmentResultId.HasValue && request.AssessmentResultId.Value > 0
            ? request.AssessmentResultId
            : null;

        var evidence = new EvidenceFile
        {
            EvidenceTypeId = request.EvidenceTypeId,
            AccountId = request.AccountId,
            SubjectResultId = request.SubjectResultId,
            AttendanceRecordId = attendanceRecordId,
            AssessmentResultId = assessmentResultId,
            VerificationStatus = "Pending", // Default value
            UploadedByAccountId = uploadedByAccountId,
            UploadedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedByAccountId = uploadedByAccountId
        };

        await _unitOfWork.EvidenceFileRepository.AddAsync(evidence, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        var attachment = new Attachment
        {
            OwnerType = OwnerType,
            OwnerId = evidence.EvidenceFileId,
            Url = request.FileUrl,
            PublicId = request.PublicId,
            FileName = request.FileName,
            MimeType = mimeType,
            FileSize = request.FileSize,
            FileHash = request.FileHash,
            UploadedByAccountId = uploadedByAccountId,
            UploadedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedByAccountId = uploadedByAccountId
        };

        await _unitOfWork.AttachmentRepository.AddAsync(attachment, cancellationToken);

        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            AccountId = uploadedByAccountId,
            ActionType = "UPLOAD",
            EntityName = nameof(EvidenceFile),
            RecordId = evidence.EvidenceFileId,
            ETRRecordId = etrForEvidence?.ETRCourseRecordId,
            NewValue = request.FileHash ?? request.FileUrl,
            Description = $"Uploaded evidence file '{request.FileName}' with hash '{request.FileHash}'"
        }, cancellationToken);

        await _unitOfWork.SaveAsync(cancellationToken);

        return MapToResponse(evidence, attachment);
    }

    public async Task<EvidenceResponse> VerifyEvidenceAsync(int id, VerifyEvidenceRequest request, int verifiedByAccountId, CancellationToken cancellationToken = default)
    {
        ValidateVerificationRequest(request.VerificationStatus, request.VerificationComment);

        var evidence = await _unitOfWork.EvidenceFileRepository.GetByIdAsync(id, cancellationToken);
        if (evidence == null)
            throw new KeyNotFoundException($"Evidence with ID {id} not found.");

        EnsureVerifierDidNotUploadEvidence(evidence, verifiedByAccountId);

        var response = await ApplyVerificationAsync(evidence, request.VerificationStatus, request.VerificationComment, verifiedByAccountId, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        if (evidence.SubjectResultId > 0)
        {
            await _assessmentResultService.EvaluateSubjectPassabilityAsync(evidence.SubjectResultId, cancellationToken);
            await _unitOfWork.SaveAsync(cancellationToken);
        }

        return response;
    }

    public async Task<BulkVerifyEvidenceResponse> BulkVerifyEvidencesAsync(BulkVerifyEvidenceRequest request, int verifiedByAccountId, CancellationToken cancellationToken = default)
    {
        // The status/comment is shared across the whole batch (QA ticks many files, picks one
        // Verified/Rejected outcome), so it is validated once upfront rather than per item.
        ValidateVerificationRequest(request.VerificationStatus, request.VerificationComment);

        var verified = new List<EvidenceResponse>();
        var failed = new List<BulkVerifyFailureItem>();

        foreach (var id in request.EvidenceIds.Distinct())
        {
            try
            {
                var evidence = await _unitOfWork.EvidenceFileRepository.GetByIdAsync(id, cancellationToken);
                if (evidence == null)
                {
                    failed.Add(new BulkVerifyFailureItem(id, "Evidence not found."));
                    continue;
                }

                EnsureVerifierDidNotUploadEvidence(evidence, verifiedByAccountId);

                verified.Add(await ApplyVerificationAsync(evidence, request.VerificationStatus, request.VerificationComment, verifiedByAccountId, cancellationToken));
            }
            catch (ForbiddenAccessException ex)
            {
                // One bad item (e.g. self-uploaded evidence) must not roll back the whole batch —
                // the rest of the QA's selection should still go through.
                failed.Add(new BulkVerifyFailureItem(id, ex.Message));
            }
        }

        if (verified.Count > 0)
        {
            await _unitOfWork.SaveAsync(cancellationToken);

            var subjectResultIds = verified
                .Select(v => v.SubjectResultId)
                .Where(sId => sId > 0)
                .Distinct();

            foreach (var sId in subjectResultIds)
            {
                await _assessmentResultService.EvaluateSubjectPassabilityAsync(sId, cancellationToken);
            }
            await _unitOfWork.SaveAsync(cancellationToken);
        }

        return new BulkVerifyEvidenceResponse(verified, failed);
    }

    private void ValidateVerificationRequest(string verificationStatus, string? verificationComment)
    {
        if (!AllowedVerificationStatuses.Contains(verificationStatus))
            throw new ValidationException($"VerificationStatus must be one of: {string.Join(", ", AllowedVerificationStatuses)}.");

        if (verificationStatus == "Rejected" && string.IsNullOrWhiteSpace(verificationComment))
            throw new ValidationException("A comment is required when rejecting evidence.");
    }

    // Segregation of duties: the person who uploaded evidence must not be the one who verifies
    // it — an independent QA review is the whole point of this gate.
    private static void EnsureVerifierDidNotUploadEvidence(EvidenceFile evidence, int verifiedByAccountId)
    {
        if (evidence.UploadedByAccountId == verifiedByAccountId)
        {
            throw new ForbiddenAccessException("You cannot verify evidence that you uploaded yourself.");
        }
    }

    private async Task<EvidenceResponse> ApplyVerificationAsync(EvidenceFile evidence, string verificationStatus, string? verificationComment, int verifiedByAccountId, CancellationToken cancellationToken)
    {
        var oldStatus = evidence.VerificationStatus;

        evidence.VerificationStatus = verificationStatus;
        evidence.VerificationComment = verificationComment;
        evidence.VerifiedByAccountId = verifiedByAccountId;
        evidence.VerifiedAt = DateTime.UtcNow;
        evidence.UpdatedAt = DateTime.UtcNow;
        evidence.UpdatedByAccountId = verifiedByAccountId;

        _unitOfWork.EvidenceFileRepository.Update(evidence);

        // One AuditLog entry PER evidence file, even in a bulk call — losing per-item traceability
        // was the exact failure mode a single "verified 90 files" log line would have caused.
        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            AccountId = verifiedByAccountId,
            ActionType = AuditActionType.VERIFY.ToString(),
            EntityName = nameof(EvidenceFile),
            RecordId = evidence.EvidenceFileId,
            OldValue = oldStatus,
            NewValue = verificationStatus,
            Description = string.IsNullOrWhiteSpace(verificationComment)
                ? $"EvidenceFile #{evidence.EvidenceFileId} {verificationStatus} by AccountId {verifiedByAccountId}"
                : $"EvidenceFile #{evidence.EvidenceFileId} {verificationStatus} by AccountId {verifiedByAccountId}. Comment: {verificationComment}"
        }, cancellationToken);

        var attachment = await GetAttachmentAsync(evidence.EvidenceFileId, cancellationToken);
        return MapToResponse(evidence, attachment);
    }

    public async Task DeleteEvidenceAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default)
    {
        var evidence = await _unitOfWork.EvidenceFileRepository.GetByIdAsync(id, cancellationToken);
        if (evidence == null)
            throw new KeyNotFoundException($"Evidence with ID {id} not found.");

        if (evidence.VerificationStatus == "Verified")
            throw new ForbiddenAccessException("Cannot delete evidence that has already been verified.");

        if (evidence.SubjectResultId > 0)
        {
            var isSignedOff = _unitOfWork.SubjectSignoffRepository.GetQueryable()
                .Any(s => s.SubjectResultId == evidence.SubjectResultId);
            
            if (isSignedOff)
                throw new ForbiddenAccessException("Cannot delete evidence for a subject result that has already been signed off.");
        }

        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            AccountId = deletedByAccountId,
            ActionType = AuditActionType.DELETE.ToString(),
            EntityName = nameof(EvidenceFile),
            RecordId = id,
            OldValue = evidence.VerificationStatus,
            NewValue = "Deleted",
            Description = $"EvidenceFile #{id} (status {evidence.VerificationStatus}) soft-deleted by AccountId {deletedByAccountId}"
        }, cancellationToken);

        // Soft delete based on BaseEntity pattern
        evidence.IsDeleted = true;
        evidence.DeletedAt = DateTime.UtcNow;
        evidence.UpdatedAt = DateTime.UtcNow;
        evidence.UpdatedByAccountId = deletedByAccountId;

        _unitOfWork.EvidenceFileRepository.Update(evidence);

        var attachment = await GetAttachmentAsync(id, cancellationToken);
        if (attachment != null)
        {
            attachment.IsDeleted = true;
            attachment.DeletedAt = DateTime.UtcNow;
            attachment.UpdatedAt = DateTime.UtcNow;
            attachment.UpdatedByAccountId = deletedByAccountId;
            _unitOfWork.AttachmentRepository.Update(attachment);
        }

        await _unitOfWork.SaveAsync(cancellationToken);
    }

    private async Task<Attachment?> GetAttachmentAsync(int evidenceFileId, CancellationToken cancellationToken)
    {
        return (await _unitOfWork.AttachmentRepository.GetAllAsync(cancellationToken))
            .FirstOrDefault(a => a.OwnerType == OwnerType && a.OwnerId == evidenceFileId);
    }

    private async Task<Dictionary<int, Attachment>> GetAttachmentsByOwnerIdsAsync(IEnumerable<int> evidenceFileIds, CancellationToken cancellationToken)
    {
        var ids = evidenceFileIds.ToHashSet();
        return (await _unitOfWork.AttachmentRepository.GetAllAsync(cancellationToken))
            .Where(a => a.OwnerType == OwnerType && ids.Contains(a.OwnerId))
            .GroupBy(a => a.OwnerId)
            .ToDictionary(g => g.Key, g => g.First());
    }

    private static EvidenceResponse MapToResponse(EvidenceFile file, Attachment? attachment)
    {
        return new EvidenceResponse
        {
            EvidenceFileId = file.EvidenceFileId,
            EvidenceTypeId = file.EvidenceTypeId,
            UploadedByAccountId = file.UploadedByAccountId,
            AccountId = file.AccountId,
            SubjectResultId = file.SubjectResultId,
            AttendanceRecordId = file.AttendanceRecordId,
            AssessmentResultId = file.AssessmentResultId,
            FileName = attachment?.FileName ?? string.Empty,
            FileUrl = attachment?.Url ?? string.Empty,
            MimeType = attachment?.MimeType,
            FileSize = attachment?.FileSize,
            FileHash = attachment?.FileHash,
            VerificationStatus = file.VerificationStatus,
            VerifiedByAccountId = file.VerifiedByAccountId,
            VerifiedAt = file.VerifiedAt,
            VerificationComment = file.VerificationComment,
            UploadedAt = file.UploadedAt
        };
    }

    private static void EnrichResponse(
        EvidenceResponse resp,
        EvidenceFile file,
        IReadOnlyDictionary<int, UserProfile> profileMap,
        IReadOnlyDictionary<int, EvidenceType> evidenceTypeMap,
        IReadOnlyDictionary<int, SubjectResult> srMap,
        IReadOnlyDictionary<int, Subject> subjectMap,
        IReadOnlyDictionary<int, ETRCourseRecord> etrMap,
        IReadOnlyDictionary<int, CourseEnrollment> enrollmentMap,
        IReadOnlyDictionary<int, Course> courseMap,
        IReadOnlyDictionary<int, Class> classMap)
    {
        // 1. Evidence Type
        if (evidenceTypeMap.TryGetValue(file.EvidenceTypeId, out var et))
        {
            resp.EvidenceTypeName = et.TypeName;
        }

        // 2. Uploaded By
        if (profileMap.TryGetValue(file.UploadedByAccountId, out var up))
        {
            resp.UploadedByName = up.FullName;
        }

        // 3. Subject Result -> Subject
        srMap.TryGetValue(file.SubjectResultId, out var sr);
        if (sr != null && subjectMap.TryGetValue(sr.SubjectId, out var s))
        {
            resp.SubjectName = s.SubjectName;
            resp.SubjectCode = s.SubjectCode;
        }

        // 4. ETR & Enrollment -> Learner, Course, Class
        ETRCourseRecord? etr = null;
        if (sr != null && etrMap.TryGetValue(sr.EtrId, out var foundEtr))
        {
            etr = foundEtr;
        }

        CourseEnrollment? enr = null;
        if (etr != null && enrollmentMap.TryGetValue(etr.EnrollmentId, out var foundEnr))
        {
            enr = foundEnr;
        }

        // Learner: prioritize AccountId on file, fallback to enrollment.AccountId
        int learnerAccId = file.AccountId > 0 ? file.AccountId : (enr?.AccountId ?? 0);
        if (profileMap.TryGetValue(learnerAccId, out var lp))
        {
            resp.LearnerName = lp.FullName;
            resp.LearnerCode = lp.UserCode;
        }
        else if (enr != null && profileMap.TryGetValue(enr.AccountId, out var enrLp))
        {
            resp.LearnerName = enrLp.FullName;
            resp.LearnerCode = enrLp.UserCode;
        }

        // Course & Class
        int classId = enr?.ClassId ?? 0;
        Class? cls = null;
        if (classId > 0 && classMap.TryGetValue(classId, out var foundCls))
        {
            cls = foundCls;
            resp.ClassName = cls.ClassName;
        }

        int courseId = cls?.CourseId ?? 0;
        if (courseId > 0 && courseMap.TryGetValue(courseId, out var course))
        {
            resp.CourseName = course.CourseName;
        }
    }
}
