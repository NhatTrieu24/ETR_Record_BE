using ETR.Application.Compliance;
using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace ETR.Application.Services;

public class EtrService : IEtrService
{
    private readonly IUnitOfWork _unitOfWork;

    public EtrService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<EtrRecordResponse>> GetAllEtrsAsync(CancellationToken cancellationToken = default)
    {
        var etrs = await _unitOfWork.ETRCourseRecordRepository.GetAllAsync(cancellationToken);
        return etrs.Select(e => new EtrRecordResponse(
            e.ETRCourseRecordId,
            e.EnrollmentId,
            e.Status,
            e.IsLocked,
            e.SubmittedAt,
            e.VerifiedAt,
            e.CompletedAt,
            e.IssuedDate,
            e.ExpiryDate,
            e.PreviousRecordId));
    }

    public async Task<IEnumerable<EtrRecordResponse>> GetMyEtrsAsync(int accountId, CancellationToken cancellationToken = default)
    {
        var enrollments = await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(cancellationToken);
        var myEnrollmentIds = enrollments.Where(e => e.AccountId == accountId).Select(e => e.EnrollmentId).ToList();

        var etrs = await _unitOfWork.ETRCourseRecordRepository.GetAllAsync(cancellationToken);
        var myEtrs = etrs.Where(e => myEnrollmentIds.Contains(e.EnrollmentId));

        return myEtrs.Select(e => new EtrRecordResponse(
            e.ETRCourseRecordId,
            e.EnrollmentId,
            e.Status,
            e.IsLocked,
            e.SubmittedAt,
            e.VerifiedAt,
            e.CompletedAt,
            e.IssuedDate,
            e.ExpiryDate,
            e.PreviousRecordId));
    }

    public async Task<EtrDetailsResponse> GetEtrByIdAsync(int etrCourseRecordId, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.ETRCourseRecordRepository.GetWithSubjectResultsAsync(etrCourseRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"ETRCourseRecord not found.");

        var subjectResults = e.SubjectResults?.Select(sr => new SubjectResultResponse(
            sr.SubjectResultId,
            sr.SubjectId,
            sr.Status,
            sr.CreatedAt,
            sr.AttendanceRate,
            sr.Score)) ?? Array.Empty<SubjectResultResponse>();

        return new EtrDetailsResponse(
            e.ETRCourseRecordId,
            e.EnrollmentId,
            e.Status,
            e.IsLocked,
            e.SubmittedAt,
            e.VerifiedAt,
            e.CompletedAt,
            subjectResults);
    }

    public async Task DeleteEtrAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default)
    {
        var etr = await _unitOfWork.ETRCourseRecordRepository.GetByIdAsync(id, cancellationToken);
        if (etr == null) throw new KeyNotFoundException("ETRCourseRecord not found.");

        etr.IsDeleted = true;
        etr.DeletedAt = DateTime.UtcNow;
        etr.UpdatedAt = DateTime.UtcNow;
        etr.UpdatedByAccountId = deletedByAccountId;

        _unitOfWork.ETRCourseRecordRepository.Update(etr);
        await _unitOfWork.SaveAsync(cancellationToken);
    }

    public async Task<EtrRecordResponse> SubmitEtrAsync(int etrCourseRecordId, int accountId, CancellationToken cancellationToken = default)
    {
        var etr = await _unitOfWork.ETRCourseRecordRepository.GetWithSubjectResultsAsync(etrCourseRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"ETRCourseRecord not found.");

        if (etr.IsLocked) throw new BusinessRuleViolationException("ETR is locked.");

        var enrollment = await _unitOfWork.CourseEnrollmentRepository.GetByIdAsync(etr.EnrollmentId, cancellationToken)
            ?? throw new BusinessRuleViolationException("Enrollment not found.");

        var trainingClass = await _unitOfWork.ClassRepository.GetByIdAsync(enrollment.ClassId, cancellationToken)
            ?? throw new BusinessRuleViolationException("Class not found.");

        var courseSubjects = (await _unitOfWork.CourseSubjectRepository.GetAllAsync(cancellationToken))
            .Where(cs => cs.CourseId == trainingClass.CourseId && cs.IsMandatory).ToList();

        // === PRE-VALIDATION ===

        // 1. Check all mandatory subjects are Passed or Exempted
        foreach (var cs in courseSubjects)
        {
            var sr = etr.SubjectResults?.FirstOrDefault(s => s.SubjectId == cs.SubjectId);
            if (sr == null || (sr.Status != "Passed" && sr.Status != "Exempted"))
            {
                throw new BusinessRuleViolationException($"Cannot submit ETR. Mandatory subject (ID: {cs.SubjectId}) is not Passed or Exempted.");
            }
        }

        // 2. Check attendance rate >= minimum threshold
        if (etr.SubjectResults != null)
        {
            foreach (var sr in etr.SubjectResults)
            {
                if ((sr.AttendanceRate ?? 0) < BusinessRuleEngine.MinimumAttendanceThreshold)
                {
                    throw new BusinessRuleViolationException($"Cannot submit ETR. Subject (ID: {sr.SubjectId}) attendance rate ({sr.AttendanceRate}%) is below minimum threshold ({BusinessRuleEngine.MinimumAttendanceThreshold}%).");
                }
            }
        }

        // 3. Check all evidence is Verified
        var allEvidences = await _unitOfWork.EvidenceFileRepository.GetAllAsync(cancellationToken);
        var etrSubjectIds = etr.SubjectResults?.Select(sr => sr.SubjectResultId).ToList() ?? new List<int>();
        var pendingEvidences = allEvidences
            .Where(e => etrSubjectIds.Contains(e.SubjectResultId) && e.VerificationStatus != "Verified" && !e.IsDeleted)
            .ToList();

        if (pendingEvidences.Any())
        {
            throw new BusinessRuleViolationException($"Cannot submit ETR. {pendingEvidences.Count} evidence file(s) are not yet Verified.");
        }

        // 4. Check all subject signoffs exist
        var allSignoffs = await _unitOfWork.SubjectSignoffRepository.GetAllAsync(cancellationToken);
        foreach (var sr in etr.SubjectResults ?? Enumerable.Empty<SubjectResult>())
        {
            var hasSignoff = allSignoffs.Any(s => s.SubjectResultId == sr.SubjectResultId);
            if (!hasSignoff)
            {
                throw new BusinessRuleViolationException($"Cannot submit ETR. Subject (ID: {sr.SubjectId}) has not been signed off by instructor.");
            }
        }

        // 5. Check mandatory CompletionRequirements configured for the course
        var completionRequirements = (await _unitOfWork.CompletionRequirementRepository.GetAllAsync(cancellationToken))
            .Where(cr => cr.CourseId == trainingClass.CourseId && cr.IsMandatory).ToList();

        foreach (var requirement in completionRequirements)
        {
            switch (requirement.RequirementType)
            {
                case "MinAttendance":
                    var minAttendance = requirement.ThresholdValue ?? BusinessRuleEngine.MinimumAttendanceThreshold;
                    if (etr.SubjectResults != null && etr.SubjectResults.Any(sr => (sr.AttendanceRate ?? 0) < minAttendance))
                    {
                        throw new BusinessRuleViolationException($"Cannot submit ETR. Completion requirement '{requirement.RequirementName}' not met: attendance below {minAttendance}%.");
                    }
                    break;

                case "AllAssessmentsPassed":
                    foreach (var cs in courseSubjects)
                    {
                        var sr = etr.SubjectResults?.FirstOrDefault(s => s.SubjectId == cs.SubjectId);
                        if (sr == null || (sr.Status != "Passed" && sr.Status != "Exempted"))
                        {
                            throw new BusinessRuleViolationException($"Cannot submit ETR. Completion requirement '{requirement.RequirementName}' not met: not all mandatory subjects are Passed or Exempted.");
                        }
                    }
                    break;

                case "AllChecklistsSignedOff":
                    var subjectResultIds = etr.SubjectResults?.Select(sr => sr.SubjectResultId).ToList() ?? new List<int>();
                    var mandatoryChecklists = (await _unitOfWork.PracticalChecklistRepository.GetAllAsync(cancellationToken))
                        .Where(pc => pc.CourseId == trainingClass.CourseId && pc.IsRequired).ToList();
                    var checklistResults = (await _unitOfWork.PracticalChecklistResultRepository.GetAllAsync(cancellationToken))
                        .Where(r => subjectResultIds.Contains(r.SubjectResultId)).ToList();

                    if (mandatoryChecklists.Any(c => !checklistResults.Any(r => r.PracticalChecklistId == c.PracticalChecklistId && r.ResultStatus == "Passed")))
                    {
                        throw new BusinessRuleViolationException($"Cannot submit ETR. Completion requirement '{requirement.RequirementName}' not met: not all mandatory practical checklists are signed off.");
                    }
                    break;

                default:
                    // Free-text/advisory requirement (RequirementType not set) — not machine-enforced.
                    break;
            }
        }

        // === AUDIT LOG ===
        var auditLog = new AuditLog
        {
            ETRRecordId = etrCourseRecordId,
            AccountId = accountId,
            ActionType = "SUBMIT",
            EntityName = nameof(ETRCourseRecord),
            RecordId = etrCourseRecordId,
            OldValue = etr.Status,
            NewValue = "Submitted",
            Description = $"ETR #{etrCourseRecordId} submitted for QA verification"
        };
        await _unitOfWork.AuditLogRepository.AddAsync(auditLog, cancellationToken);

        etr.Status = "Submitted";
        etr.SubmittedAt = DateTime.UtcNow;
        etr.UpdatedAt = DateTime.UtcNow;
        etr.UpdatedByAccountId = accountId;

        _unitOfWork.ETRCourseRecordRepository.Update(etr);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new EtrRecordResponse(etr.ETRCourseRecordId, etr.EnrollmentId, etr.Status, etr.IsLocked, etr.SubmittedAt, etr.VerifiedAt, etr.CompletedAt, etr.IssuedDate, etr.ExpiryDate, etr.PreviousRecordId);
    }

    public async Task<EtrRecordResponse> VerifyEtrAsync(int etrCourseRecordId, int accountId, CancellationToken cancellationToken = default)
    {
        var etr = await _unitOfWork.ETRCourseRecordRepository.GetByIdAsync(etrCourseRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"ETRCourseRecord not found.");

        if (etr.Status != "Submitted")
            throw new BusinessRuleViolationException("Cannot verify ETR that is not in Submitted status.");

        // === AUDIT LOG ===
        var auditLog = new AuditLog
        {
            ETRRecordId = etrCourseRecordId,
            AccountId = accountId,
            ActionType = "VERIFY",
            EntityName = nameof(ETRCourseRecord),
            RecordId = etrCourseRecordId,
            OldValue = etr.Status,
            NewValue = "Verified",
            Description = $"ETR #{etrCourseRecordId} verified by QA"
        };
        await _unitOfWork.AuditLogRepository.AddAsync(auditLog, cancellationToken);

        etr.Status = "Verified";
        etr.VerifiedAt = DateTime.UtcNow;
        etr.UpdatedAt = DateTime.UtcNow;
        etr.UpdatedByAccountId = accountId;

        _unitOfWork.ETRCourseRecordRepository.Update(etr);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new EtrRecordResponse(etr.ETRCourseRecordId, etr.EnrollmentId, etr.Status, etr.IsLocked, etr.SubmittedAt, etr.VerifiedAt, etr.CompletedAt, etr.IssuedDate, etr.ExpiryDate, etr.PreviousRecordId);
    }

    public async Task<EtrRecordResponse> ReturnEtrAsync(int etrCourseRecordId, int accountId, string? comment, CancellationToken cancellationToken = default)
    {
        var etr = await _unitOfWork.ETRCourseRecordRepository.GetByIdAsync(etrCourseRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"ETRCourseRecord not found.");

        if (etr.Status != "Submitted")
            throw new BusinessRuleViolationException("Cannot return ETR that is not in Submitted status.");

        if (string.IsNullOrWhiteSpace(comment))
            throw new ValidationException("A comment is required when returning an ETR for correction.");

        // === AUDIT LOG ===
        var auditLog = new AuditLog
        {
            ETRRecordId = etrCourseRecordId,
            AccountId = accountId,
            ActionType = "RETURN",
            EntityName = nameof(ETRCourseRecord),
            RecordId = etrCourseRecordId,
            OldValue = etr.Status,
            NewValue = "ReturnedForCorrection",
            Description = $"ETR #{etrCourseRecordId} returned for correction by QA. Comment: {comment ?? "N/A"}"
        };
        await _unitOfWork.AuditLogRepository.AddAsync(auditLog, cancellationToken);

        etr.Status = "ReturnedForCorrection";
        etr.UpdatedAt = DateTime.UtcNow;
        etr.UpdatedByAccountId = accountId;

        _unitOfWork.ETRCourseRecordRepository.Update(etr);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new EtrRecordResponse(etr.ETRCourseRecordId, etr.EnrollmentId, etr.Status, etr.IsLocked, etr.SubmittedAt, etr.VerifiedAt, etr.CompletedAt, etr.IssuedDate, etr.ExpiryDate, etr.PreviousRecordId);
    }

    public async Task<EtrRecordResponse> CompleteEtrAsync(int etrCourseRecordId, int accountId, CancellationToken cancellationToken = default)
    {
        var etr = await _unitOfWork.ETRCourseRecordRepository.GetWithSubjectResultsAsync(etrCourseRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"ETRCourseRecord not found.");

        if (etr.Status != "Verified")
            throw new BusinessRuleViolationException("Cannot complete ETR that is not in Verified status.");

        var enrollment = await _unitOfWork.CourseEnrollmentRepository.GetByIdAsync(etr.EnrollmentId, cancellationToken);
        if (enrollment == null) throw new BusinessRuleViolationException("Enrollment not found.");

        var trainingClass = await _unitOfWork.ClassRepository.GetByIdAsync(enrollment.ClassId, cancellationToken);
        if (trainingClass == null) throw new BusinessRuleViolationException("Class not found.");

        var courseSubjects = (await _unitOfWork.CourseSubjectRepository.GetAllAsync(cancellationToken))
            .Where(cs => cs.CourseId == trainingClass.CourseId && cs.IsMandatory).ToList();

        // === PRE-VALIDATION ===

        // 1. Check all mandatory subjects are Passed or Exempted
        foreach (var cs in courseSubjects)
        {
            var sr = etr.SubjectResults.FirstOrDefault(s => s.SubjectId == cs.SubjectId);
            if (sr == null || (sr.Status != "Passed" && sr.Status != "Exempted"))
            {
                throw new BusinessRuleViolationException($"Cannot complete ETR. Mandatory subject (ID: {cs.SubjectId}) is not Passed or Exempted.");
            }
        }

        // 2. Check all evidence is Verified
        var allEvidences = await _unitOfWork.EvidenceFileRepository.GetAllAsync(cancellationToken);
        var etrSubjectIds = etr.SubjectResults?.Select(sr => sr.SubjectResultId).ToList() ?? new List<int>();
        var pendingEvidences = allEvidences
            .Where(e => etrSubjectIds.Contains(e.SubjectResultId) && e.VerificationStatus != "Verified" && !e.IsDeleted)
            .ToList();

        if (pendingEvidences.Any())
        {
            throw new BusinessRuleViolationException($"Cannot complete ETR. {pendingEvidences.Count} evidence file(s) are not yet Verified.");
        }

        // === AUDIT LOG ===
        var auditLog = new AuditLog
        {
            ETRRecordId = etrCourseRecordId,
            AccountId = accountId,
            ActionType = "APPROVE",
            EntityName = nameof(ETRCourseRecord),
            RecordId = etrCourseRecordId,
            OldValue = etr.Status,
            NewValue = "Completed",
            Description = $"ETR #{etrCourseRecordId} completed and locked by Training Manager"
        };
        await _unitOfWork.AuditLogRepository.AddAsync(auditLog, cancellationToken);

        var course = await _unitOfWork.CourseRepository.GetByIdAsync(trainingClass.CourseId, cancellationToken);

        etr.Status = "Completed";
        etr.CompletedAt = DateTime.UtcNow;
        etr.IsLocked = true;
        etr.IssuedDate = DateTime.UtcNow;
        if (course != null && course.ValidityMonths.HasValue)
        {
            etr.ExpiryDate = DateTime.UtcNow.AddMonths(course.ValidityMonths.Value);
        }
        etr.UpdatedAt = DateTime.UtcNow;
        etr.UpdatedByAccountId = accountId;

        // Update enrollment completion date
        enrollment.ActualCompletionDate = DateTime.UtcNow;
        _unitOfWork.CourseEnrollmentRepository.Update(enrollment);

        // Keep ApprovalHistory in sync even when completion happens via this direct route rather than
        // ApprovalService.ProcessApprovalActionAsync(action:"Approve") — ExportService.BuildAuditHistoryPdf
        // (the CAA audit export) reads exclusively from ApprovalHistory, so without this the exported
        // Audit_History.pdf would silently be missing the final approval entry.
        var approvalRequest = (await _unitOfWork.ApprovalRequestRepository.GetAllAsync(cancellationToken))
            .FirstOrDefault(a => a.ETRCourseRecordId == etrCourseRecordId);
        if (approvalRequest != null && approvalRequest.CurrentStatus != "Approved")
        {
            var previousApprovalStatus = approvalRequest.CurrentStatus;
            approvalRequest.CurrentStatus = "Approved";
            approvalRequest.CompletedAt = DateTime.UtcNow;
            approvalRequest.UpdatedAt = DateTime.UtcNow;
            approvalRequest.UpdatedByAccountId = accountId;
            _unitOfWork.ApprovalRequestRepository.Update(approvalRequest);

            await _unitOfWork.ApprovalHistoryRepository.AddAsync(new ApprovalHistory
            {
                ApprovalRequestId = approvalRequest.ApprovalRequestId,
                ActionByAccountId = accountId,
                ActionType = "Approve",
                PreviousStatus = previousApprovalStatus,
                NewStatus = "Approved",
                Comments = "ETR completed directly via /api/etr/{id}/complete",
                ActionAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedByAccountId = accountId
            }, cancellationToken);
        }

        _unitOfWork.ETRCourseRecordRepository.Update(etr);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new EtrRecordResponse(etr.ETRCourseRecordId, etr.EnrollmentId, etr.Status, etr.IsLocked, etr.SubmittedAt, etr.VerifiedAt, etr.CompletedAt, etr.IssuedDate, etr.ExpiryDate, etr.PreviousRecordId);
    }

    public async Task<EtrRecordResponse> LockEtrAsync(int etrCourseRecordId, int accountId, string? reason, CancellationToken cancellationToken = default)
    {
        var etr = await _unitOfWork.ETRCourseRecordRepository.GetByIdAsync(etrCourseRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"ETRCourseRecord not found.");

        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            ETRRecordId = etrCourseRecordId,
            AccountId = accountId,
            ActionType = "LOCK",
            EntityName = nameof(ETRCourseRecord),
            RecordId = etrCourseRecordId,
            OldValue = etr.IsLocked.ToString(),
            NewValue = "True",
            Description = $"ETR #{etrCourseRecordId} manually locked. Reason: {reason ?? "N/A"}"
        }, cancellationToken);

        // No explicit Update(etr) call here: GetByIdAsync above already tracks this entity in the
        // same DbContext, so EF Core's own change detection picks up the mutation automatically.
        // Calling Update() (DbSet.Update) would force EVERY property to IsModified=true, not just
        // IsLocked — which breaks ImmutabilityValidator's fine-grained IsBeingUnlocked check below.
        etr.IsLocked = true;
        etr.UpdatedAt = DateTime.UtcNow;
        etr.UpdatedByAccountId = accountId;

        await _unitOfWork.SaveAsync(cancellationToken);

        return new EtrRecordResponse(etr.ETRCourseRecordId, etr.EnrollmentId, etr.Status, etr.IsLocked, etr.SubmittedAt, etr.VerifiedAt, etr.CompletedAt, etr.IssuedDate, etr.ExpiryDate, etr.PreviousRecordId);
    }

    public async Task<EtrRecordResponse> UnlockEtrAsync(int etrCourseRecordId, int accountId, string? reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ValidationException("A reason is required to re-open a locked ETR.");

        var etr = await _unitOfWork.ETRCourseRecordRepository.GetByIdAsync(etrCourseRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"ETRCourseRecord not found.");

        if (!etr.IsLocked)
            throw new BusinessRuleViolationException("ETR is not locked.");

        // Re-opening a Completed/Locked ETR is the FRD's explicitly-named exception to absolute
        // immutability — it must always leave a full audit trail, since it's the one path that lets
        // previously-frozen data become editable again.
        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            ETRRecordId = etrCourseRecordId,
            AccountId = accountId,
            ActionType = "UNLOCK",
            EntityName = nameof(ETRCourseRecord),
            RecordId = etrCourseRecordId,
            OldValue = "True",
            NewValue = "False",
            Description = $"ETR #{etrCourseRecordId} re-opened (unlocked). Reason: {reason}"
        }, cancellationToken);

        // No explicit Update(etr) call — see LockEtrAsync above for why: it would mark every
        // property Modified and defeat ImmutabilityValidator's IsBeingUnlocked check, which requires
        // ONLY IsLocked to have changed for the re-open exception to apply.
        etr.IsLocked = false;
        etr.UpdatedAt = DateTime.UtcNow;
        etr.UpdatedByAccountId = accountId;

        await _unitOfWork.SaveAsync(cancellationToken);

        return new EtrRecordResponse(etr.ETRCourseRecordId, etr.EnrollmentId, etr.Status, etr.IsLocked, etr.SubmittedAt, etr.VerifiedAt, etr.CompletedAt, etr.IssuedDate, etr.ExpiryDate, etr.PreviousRecordId);
    }

    public async Task<IEnumerable<EtrRecordResponse>> GetStudentEtrHistoryAsync(int studentId, CancellationToken cancellationToken = default)
    {
        var enrollments = await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(cancellationToken);
        var studentEnrollmentIds = enrollments.Where(e => e.AccountId == studentId).Select(e => e.EnrollmentId).ToList();

        var etrs = await _unitOfWork.ETRCourseRecordRepository.GetAllAsync(cancellationToken);
        return etrs
            .Where(e => studentEnrollmentIds.Contains(e.EnrollmentId))
            .OrderByDescending(e => e.IssuedDate ?? e.CreatedAt)
            .Select(e => new EtrRecordResponse(
                e.ETRCourseRecordId,
                e.EnrollmentId,
                e.Status,
                e.IsLocked,
                e.SubmittedAt,
                e.VerifiedAt,
                e.CompletedAt,
                e.IssuedDate,
                e.ExpiryDate,
                e.PreviousRecordId));
    }

    public async Task<IEnumerable<StudentEtrStatusResponse>> GetStudentEtrCurrentStatusAsync(int studentId, CancellationToken cancellationToken = default)
    {
        var enrollments = await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(cancellationToken);
        var studentEnrollments = enrollments.Where(e => e.AccountId == studentId).ToList();
        
        var classes = await _unitOfWork.ClassRepository.GetAllAsync(cancellationToken);
        var courses = await _unitOfWork.CourseRepository.GetAllAsync(cancellationToken);
        
        var etrs = await _unitOfWork.ETRCourseRecordRepository.GetAllAsync(cancellationToken);
        
        var result = new List<StudentEtrStatusResponse>();
        var groupedByCourse = studentEnrollments
            .Join(classes, e => e.ClassId, c => c.ClassId, (e, c) => new { e.EnrollmentId, c.CourseId })
            .Join(etrs, ec => ec.EnrollmentId, etr => etr.EnrollmentId, (ec, etr) => new { ec.CourseId, Etr = etr })
            .GroupBy(x => x.CourseId);

        foreach (var group in groupedByCourse)
        {
            var latestEtr = group.OrderByDescending(x => x.Etr.IssuedDate ?? x.Etr.CreatedAt).First().Etr;
            var course = courses.FirstOrDefault(c => c.CourseId == group.Key);
            
            string validityStatus = "Valid";
            if (latestEtr.ExpiryDate.HasValue)
            {
                if (latestEtr.ExpiryDate.Value < DateTime.UtcNow)
                {
                    validityStatus = "Expired";
                }
                else if ((latestEtr.ExpiryDate.Value - DateTime.UtcNow).TotalDays <= 30)
                {
                    validityStatus = "ExpiringSoon";
                }
            }
            
            result.Add(new StudentEtrStatusResponse(
                group.Key,
                course?.CourseName ?? "Unknown",
                latestEtr.ETRCourseRecordId,
                latestEtr.IssuedDate,
                latestEtr.ExpiryDate,
                validityStatus
            ));
        }

        return result;
    }

    public async Task<IEnumerable<ExpiringStudentResponse>> GetExpiringStudentsAsync(int courseId, int daysThreshold, CancellationToken cancellationToken = default)
    {
        var enrollments = await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(cancellationToken);
        var classes = (await _unitOfWork.ClassRepository.GetAllAsync(cancellationToken)).Where(c => c.CourseId == courseId).ToList();
        var classIds = classes.Select(c => c.ClassId).ToList();
        var courseEnrollments = enrollments.Where(e => classIds.Contains(e.ClassId)).ToList();
        
        var etrs = await _unitOfWork.ETRCourseRecordRepository.GetAllAsync(cancellationToken);
        var accounts = await _unitOfWork.AccountRepository.GetAllAsync(cancellationToken);
        var userProfiles = await _unitOfWork.UserProfileRepository.GetAllAsync(cancellationToken);
        
        var result = new List<ExpiringStudentResponse>();
        
        var groupedByStudent = courseEnrollments
            .Join(etrs, e => e.EnrollmentId, etr => etr.EnrollmentId, (e, etr) => new { e.AccountId, Etr = etr })
            .GroupBy(x => x.AccountId);

        foreach (var group in groupedByStudent)
        {
            var latestEtr = group.OrderByDescending(x => x.Etr.IssuedDate ?? x.Etr.CreatedAt).First().Etr;
            if (latestEtr.ExpiryDate.HasValue)
            {
                var daysUntilExpiry = (latestEtr.ExpiryDate.Value - DateTime.UtcNow).TotalDays;
                if (daysUntilExpiry <= daysThreshold)
                {
                    var account = accounts.FirstOrDefault(a => a.AccountId == group.Key);
                    var profile = userProfiles.FirstOrDefault(p => p.AccountId == group.Key);
                    
                    string validityStatus = daysUntilExpiry < 0 ? "Expired" : "ExpiringSoon";
                    
                    result.Add(new ExpiringStudentResponse(
                        group.Key,
                        account?.Username ?? "Unknown",
                        profile?.FullName ?? "Unknown",
                        courseId,
                        latestEtr.ETRCourseRecordId,
                        latestEtr.ExpiryDate,
                        validityStatus
                    ));
                }
            }
        }

        return result;
    }
}