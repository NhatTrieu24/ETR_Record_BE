using ETR.Application.Compliance;
using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;

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
            e.CompletedAt));
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
            e.CompletedAt));
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
            sr.AttendanceRate)) ?? Array.Empty<SubjectResultResponse>();

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

        if (etr.IsLocked) throw new InvalidOperationException("ETR is locked.");

        var enrollment = await _unitOfWork.CourseEnrollmentRepository.GetByIdAsync(etr.EnrollmentId, cancellationToken)
            ?? throw new InvalidOperationException("Enrollment not found.");

        var trainingClass = await _unitOfWork.ClassRepository.GetByIdAsync(enrollment.ClassId, cancellationToken)
            ?? throw new InvalidOperationException("Class not found.");

        var courseSubjects = (await _unitOfWork.CourseSubjectRepository.GetAllAsync(cancellationToken))
            .Where(cs => cs.CourseId == trainingClass.CourseId && cs.IsMandatory).ToList();

        // === PRE-VALIDATION ===

        // 1. Check all mandatory subjects are Passed or Exempted
        foreach (var cs in courseSubjects)
        {
            var sr = etr.SubjectResults?.FirstOrDefault(s => s.SubjectId == cs.SubjectId);
            if (sr == null || (sr.Status != "Passed" && sr.Status != "Exempted"))
            {
                throw new InvalidOperationException($"Cannot submit ETR. Mandatory subject (ID: {cs.SubjectId}) is not Passed or Exempted.");
            }
        }

        // 2. Check attendance rate >= minimum threshold
        if (etr.SubjectResults != null)
        {
            foreach (var sr in etr.SubjectResults)
            {
                if ((sr.AttendanceRate ?? 0) < BusinessRuleEngine.MinimumAttendanceThreshold)
                {
                    throw new InvalidOperationException($"Cannot submit ETR. Subject (ID: {sr.SubjectId}) attendance rate ({sr.AttendanceRate}%) is below minimum threshold ({BusinessRuleEngine.MinimumAttendanceThreshold}%).");
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
            throw new InvalidOperationException($"Cannot submit ETR. {pendingEvidences.Count} evidence file(s) are not yet Verified.");
        }

        // 4. Check all subject signoffs exist
        var allSignoffs = await _unitOfWork.SubjectSignoffRepository.GetAllAsync(cancellationToken);
        foreach (var sr in etr.SubjectResults ?? Enumerable.Empty<SubjectResult>())
        {
            var hasSignoff = allSignoffs.Any(s => s.SubjectResultId == sr.SubjectResultId);
            if (!hasSignoff)
            {
                throw new InvalidOperationException($"Cannot submit ETR. Subject (ID: {sr.SubjectId}) has not been signed off by instructor.");
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

        return new EtrRecordResponse(etr.ETRCourseRecordId, etr.EnrollmentId, etr.Status, etr.IsLocked, etr.SubmittedAt, etr.VerifiedAt, etr.CompletedAt);
    }

    public async Task<EtrRecordResponse> VerifyEtrAsync(int etrCourseRecordId, int accountId, CancellationToken cancellationToken = default)
    {
        var etr = await _unitOfWork.ETRCourseRecordRepository.GetByIdAsync(etrCourseRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"ETRCourseRecord not found.");

        if (etr.Status != "Submitted")
            throw new InvalidOperationException("Cannot verify ETR that is not in Submitted status.");

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

        return new EtrRecordResponse(etr.ETRCourseRecordId, etr.EnrollmentId, etr.Status, etr.IsLocked, etr.SubmittedAt, etr.VerifiedAt, etr.CompletedAt);
    }

    public async Task<EtrRecordResponse> ReturnEtrAsync(int etrCourseRecordId, int accountId, string? comment, CancellationToken cancellationToken = default)
    {
        var etr = await _unitOfWork.ETRCourseRecordRepository.GetByIdAsync(etrCourseRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"ETRCourseRecord not found.");

        if (etr.Status != "Submitted")
            throw new InvalidOperationException("Cannot return ETR that is not in Submitted status.");

        // === AUDIT LOG ===
        var auditLog = new AuditLog
        {
            ETRRecordId = etrCourseRecordId,
            AccountId = accountId,
            ActionType = "RETURN",
            EntityName = nameof(ETRCourseRecord),
            RecordId = etrCourseRecordId,
            OldValue = etr.Status,
            NewValue = "Draft",
            Description = $"ETR #{etrCourseRecordId} returned for correction by QA. Comment: {comment ?? "N/A"}"
        };
        await _unitOfWork.AuditLogRepository.AddAsync(auditLog, cancellationToken);

        etr.Status = "Draft";
        etr.UpdatedAt = DateTime.UtcNow;
        etr.UpdatedByAccountId = accountId;

        _unitOfWork.ETRCourseRecordRepository.Update(etr);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new EtrRecordResponse(etr.ETRCourseRecordId, etr.EnrollmentId, etr.Status, etr.IsLocked, etr.SubmittedAt, etr.VerifiedAt, etr.CompletedAt);
    }

    public async Task<EtrRecordResponse> CompleteEtrAsync(int etrCourseRecordId, int accountId, CancellationToken cancellationToken = default)
    {
        var etr = await _unitOfWork.ETRCourseRecordRepository.GetWithSubjectResultsAsync(etrCourseRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"ETRCourseRecord not found.");

        if (etr.Status != "Verified")
            throw new InvalidOperationException("Cannot complete ETR that is not in Verified status.");

        var enrollment = await _unitOfWork.CourseEnrollmentRepository.GetByIdAsync(etr.EnrollmentId, cancellationToken);
        if (enrollment == null) throw new InvalidOperationException("Enrollment not found.");

        var trainingClass = await _unitOfWork.ClassRepository.GetByIdAsync(enrollment.ClassId, cancellationToken);
        if (trainingClass == null) throw new InvalidOperationException("Class not found.");

        var courseSubjects = (await _unitOfWork.CourseSubjectRepository.GetAllAsync(cancellationToken))
            .Where(cs => cs.CourseId == trainingClass.CourseId && cs.IsMandatory).ToList();

        // === PRE-VALIDATION ===

        // 1. Check all mandatory subjects are Passed or Exempted
        foreach (var cs in courseSubjects)
        {
            var sr = etr.SubjectResults.FirstOrDefault(s => s.SubjectId == cs.SubjectId);
            if (sr == null || (sr.Status != "Passed" && sr.Status != "Exempted"))
            {
                throw new InvalidOperationException($"Cannot complete ETR. Mandatory subject (ID: {cs.SubjectId}) is not Passed or Exempted.");
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
            throw new InvalidOperationException($"Cannot complete ETR. {pendingEvidences.Count} evidence file(s) are not yet Verified.");
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

        etr.Status = "Completed";
        etr.CompletedAt = DateTime.UtcNow;
        etr.IsLocked = true;
        etr.UpdatedAt = DateTime.UtcNow;
        etr.UpdatedByAccountId = accountId;

        // Update enrollment completion date
        enrollment.ActualCompletionDate = DateTime.UtcNow;
        _unitOfWork.CourseEnrollmentRepository.Update(enrollment);

        _unitOfWork.ETRCourseRecordRepository.Update(etr);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new EtrRecordResponse(etr.ETRCourseRecordId, etr.EnrollmentId, etr.Status, etr.IsLocked, etr.SubmittedAt, etr.VerifiedAt, etr.CompletedAt);
    }

    public async Task<EtrRecordResponse> LockEtrAsync(int etrCourseRecordId, int accountId, CancellationToken cancellationToken = default)
    {
        var etr = await _unitOfWork.ETRCourseRecordRepository.GetByIdAsync(etrCourseRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"ETRCourseRecord not found.");

        etr.IsLocked = true;
        etr.UpdatedAt = DateTime.UtcNow;
        etr.UpdatedByAccountId = accountId;

        _unitOfWork.ETRCourseRecordRepository.Update(etr);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new EtrRecordResponse(etr.ETRCourseRecordId, etr.EnrollmentId, etr.Status, etr.IsLocked, etr.SubmittedAt, etr.VerifiedAt, etr.CompletedAt);
    }

    public async Task<EtrRecordResponse> UnlockEtrAsync(int etrCourseRecordId, int accountId, CancellationToken cancellationToken = default)
    {
        var etr = await _unitOfWork.ETRCourseRecordRepository.GetByIdAsync(etrCourseRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"ETRCourseRecord not found.");

        etr.IsLocked = false;
        etr.UpdatedAt = DateTime.UtcNow;
        etr.UpdatedByAccountId = accountId;

        _unitOfWork.ETRCourseRecordRepository.Update(etr);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new EtrRecordResponse(etr.ETRCourseRecordId, etr.EnrollmentId, etr.Status, etr.IsLocked, etr.SubmittedAt, etr.VerifiedAt, etr.CompletedAt);
    }
}