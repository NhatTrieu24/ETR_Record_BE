using ETR.Application.Compliance;
using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using ETR.Domain.Enums;

namespace ETR.Application.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public EnrollmentService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    private async Task<HashSet<int>> GetInstructorClassIdsAsync(int instructorAccountId, CancellationToken cancellationToken)
    {
        return _unitOfWork.ClassSubjectRepository.GetQueryable()
            .Where(cs => cs.InstructorAccountId == instructorAccountId)
            .Select(cs => cs.ClassId)
            .ToHashSet();
    }

    public async Task<IEnumerable<EnrollmentResponse>> GetAllEnrollmentsAsync(CancellationToken cancellationToken = default)
    {
        var enrollments = await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(cancellationToken);
        
        if (_currentUserService.RoleName == "Instructor" && _currentUserService.AccountId.HasValue)
        {
            var myClassIds = await GetInstructorClassIdsAsync(_currentUserService.AccountId.Value, cancellationToken);
            enrollments = enrollments.Where(e => myClassIds.Contains(e.ClassId)).ToList();
        }
        return enrollments.Select(e => new EnrollmentResponse(
            e.EnrollmentId,
            e.AccountId,
            e.ClassId,
            e.Status,
            e.EnrolledAt));
    }

    public async Task<EnrollmentResponse> GetEnrollmentByIdAsync(int enrollmentId, CancellationToken cancellationToken = default)
    {
        var e = await _unitOfWork.CourseEnrollmentRepository.GetByIdAsync(enrollmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Enrollment not found");

        if (_currentUserService.RoleName == "Instructor" && _currentUserService.AccountId.HasValue)
        {
            var myClassIds = await GetInstructorClassIdsAsync(_currentUserService.AccountId.Value, cancellationToken);
            if (!myClassIds.Contains(e.ClassId))
            {
                throw new KeyNotFoundException("Enrollment not found");
            }
        }

        return new EnrollmentResponse(
            e.EnrollmentId,
            e.AccountId,
            e.ClassId,
            e.Status,
            e.EnrolledAt);
    }

    public async Task<IEnumerable<EnrollmentResponse>> GetEnrollmentsByStudentIdAsync(int studentId, CancellationToken cancellationToken = default)
    {
        var enrollments = await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(cancellationToken);
        
        if (_currentUserService.RoleName == "Instructor" && _currentUserService.AccountId.HasValue)
        {
            var myClassIds = await GetInstructorClassIdsAsync(_currentUserService.AccountId.Value, cancellationToken);
            enrollments = enrollments.Where(e => myClassIds.Contains(e.ClassId)).ToList();
        }
        
        return enrollments
            .Where(e => e.AccountId == studentId)
            .Select(e => new EnrollmentResponse(
                e.EnrollmentId,
                e.AccountId,
                e.ClassId,
                e.Status,
                e.EnrolledAt));
    }

    public async Task<CreateEnrollmentResponse> CreateEnrollmentAsync(
        int accountId,
        int classId,
        int createdByAccountId,
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInStrategyAsync(async (ct) =>
        {
            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var result = await CreateEnrollmentCoreAsync(accountId, classId, createdByAccountId, ct);
                await _unitOfWork.CommitTransactionAsync(ct);
                return result;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }, cancellationToken);
    }

    public async Task<CreateEnrollmentResponse> CreateEnrollmentCoreAsync(
        int accountId,
        int classId,
        int createdByAccountId,
        CancellationToken cancellationToken = default)
    {
        var ct = cancellationToken;

        var userProfile = await _unitOfWork.UserProfileRepository.GetByIdAsync(accountId, ct);
        if (userProfile == null)
        {
            throw new BusinessRuleViolationException($"Cannot enroll. Learner (Account ID: {accountId}) does not have a complete user profile.");
        }

                var trainingClass = await _unitOfWork.ClassRepository.GetByIdAsync(classId, ct);
                if (trainingClass == null) throw new BusinessRuleViolationException("Class not found.");

                var course = await _unitOfWork.CourseRepository.GetByIdAsync(trainingClass.CourseId, ct);

                // === BUSINESS RULE 1: Course must have at least one Subject before enrollment ===
                var hasSubjectsFromCheck = (await _unitOfWork.CourseSubjectRepository.GetAllAsync(ct)).Any(cs => cs.CourseId == trainingClass.CourseId);
                if (!hasSubjectsFromCheck)
                {
                    throw new BusinessRuleViolationException($"Cannot enroll. Course (ID: {trainingClass.CourseId}) has no subjects configured. Please add subjects to the course first.");
                }

                var allEtrs = await _unitOfWork.ETRCourseRecordRepository.GetAllAsync(ct);
                var allEnrollments = await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(ct);
                var allClasses = await _unitOfWork.ClassRepository.GetAllAsync(ct);

                var studentEnrollments = allEnrollments.Where(e => e.AccountId == accountId).ToList();
                
                if (studentEnrollments.Any(e => e.ClassId == classId && e.Status != EnrollmentStatus.Deleted))
                {
                    throw new BusinessRuleViolationException("Learner is already enrolled in this exact class.");
                }

                var studentEtrsForCourse = allEtrs
                    .Where(etr => studentEnrollments.Any(e => e.EnrollmentId == etr.EnrollmentId && 
                                  allClasses.Any(c => c.ClassId == e.ClassId && c.CourseId == trainingClass.CourseId)))
                    .ToList();

                if (studentEtrsForCourse.Any(etr => !etr.IsLocked))
                {
                    throw new BusinessRuleViolationException("Learner is already enrolled in an active class for this course and has an ongoing ETR.");
                }

                var previousEtr = studentEtrsForCourse.OrderByDescending(etr => etr.CompletedAt ?? DateTime.MinValue).FirstOrDefault();

                var enrollment = new CourseEnrollment
                {
                    AccountId = accountId,
                    ClassId = classId,
                    Status = EnrollmentStatus.Active,
                    EnrolledAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByAccountId = createdByAccountId
                };

                await _unitOfWork.CourseEnrollmentRepository.AddAsync(enrollment, ct);
                await _unitOfWork.SaveAsync(ct);

                var etrRecord = new ETRCourseRecord
                {
                    EnrollmentId = enrollment.EnrollmentId,
                    Status = EtrStatus.InProgress,
                    IsLocked = false,
                    CreatedBySystem = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByAccountId = createdByAccountId,
                    PreviousRecordId = previousEtr?.ETRCourseRecordId,
                    CourseVersionNo = course?.VersionNo ?? 1
                };

                await _unitOfWork.ETRCourseRecordRepository.AddAsync(etrRecord, ct);
                await _unitOfWork.SaveAsync(ct);

                // NOTE: re-enrolling does NOT clear Grounded by itself — merely being back in a
                // class is not "fit for duty" for aviation certification purposes. Grounded is only
                // cleared once this new ETR is actually Completed — see EtrService.CompleteEtrAsync.

                var courseSubjects = (await _unitOfWork.CourseSubjectRepository.GetAllAsync(ct))
                    .Where(cs => cs.CourseId == trainingClass.CourseId).ToList();

                var allAssessments = (await _unitOfWork.AssessmentRepository.GetAllAsync(ct))
                    .Where(a => a.CourseId == trainingClass.CourseId).ToList();
                var allChecklists = (await _unitOfWork.PracticalChecklistRepository.GetAllAsync(ct))
                    .Where(p => p.CourseId == trainingClass.CourseId).ToList();

                // Retake-only-failed-subjects (mục 1.2): a subject the learner already Passed/Exempted
                // on the previous attempt is carried over unchanged instead of being reset to Pending —
                // only subjects that were NOT passed require retaking. Also carries over the underlying
                // AssessmentResult/PracticalChecklistResult rows so EtrService.SubmitEtrAsync/
                // GetCompletionProgressAsync (which check "all assessments/checklists passed" against
                // THIS ETR's own child rows) see the carried-over subject as already satisfied, without
                // needing any change to that validation logic.
                var previousSubjectResults = previousEtr != null
                    ? (await _unitOfWork.SubjectResultRepository.GetAllAsync(ct))
                        .Where(sr => sr.EtrId == previousEtr.ETRCourseRecordId)
                        .ToList()
                    : new List<SubjectResult>();
                var previousAssessmentResults = previousEtr != null
                    ? (await _unitOfWork.AssessmentResultRepository.GetAllAsync(ct))
                        .Where(ar => previousSubjectResults.Select(sr => sr.SubjectResultId).Contains(ar.SubjectResultId))
                        .ToList()
                    : new List<AssessmentResult>();
                var previousChecklistResults = previousEtr != null
                    ? (await _unitOfWork.PracticalChecklistResultRepository.GetAllAsync(ct))
                        .Where(pr => previousSubjectResults.Select(sr => sr.SubjectResultId).Contains(pr.SubjectResultId))
                        .ToList()
                    : new List<PracticalChecklistResult>();

                foreach (var cs in courseSubjects)
                {
                    var previousSubjectResult = previousSubjectResults.FirstOrDefault(sr => sr.SubjectId == cs.SubjectId);
                    var isCarriedOver = previousSubjectResult != null
                        && (previousSubjectResult.Status == SubjectResultStatus.Passed || previousSubjectResult.Status == SubjectResultStatus.Exempted);

                    var subjectResult = new SubjectResult
                    {
                        EtrId = etrRecord.ETRCourseRecordId,
                        CourseId = cs.CourseId,
                        SubjectId = cs.SubjectId,
                        Status = isCarriedOver ? previousSubjectResult!.Status : SubjectResultStatus.Pending,
                        AttendanceRate = isCarriedOver ? previousSubjectResult!.AttendanceRate : null,
                        Score = isCarriedOver ? previousSubjectResult!.Score : null,
                        EvaluatedByAccountId = isCarriedOver ? previousSubjectResult!.EvaluatedByAccountId : null,
                        EvaluatedAt = isCarriedOver ? previousSubjectResult!.EvaluatedAt : null,
                        CarriedOverFromSubjectResultId = isCarriedOver ? previousSubjectResult!.SubjectResultId : null,
                        PassingScoreSnapshot = cs.PassingScore,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByAccountId = createdByAccountId
                    };
                    await _unitOfWork.SubjectResultRepository.AddAsync(subjectResult, ct);
                    await _unitOfWork.SaveAsync(ct);

                    var subjectAssessments = allAssessments.Where(a => a.SubjectId == cs.SubjectId).ToList();
                    foreach (var assessment in subjectAssessments)
                    {
                        var previousAssessmentResult = isCarriedOver
                            ? previousAssessmentResults.FirstOrDefault(ar => ar.AssessmentId == assessment.AssessmentId && ar.SubjectResultId == previousSubjectResult!.SubjectResultId)
                            : null;

                        var assessmentResult = new AssessmentResult
                        {
                            AssessmentId = assessment.AssessmentId,
                            AccountId = accountId,
                            SubjectResultId = subjectResult.SubjectResultId,
                            Score = previousAssessmentResult?.Score ?? 0,
                            ResultStatus = previousAssessmentResult?.ResultStatus ?? "Pending",
                            GradedByAccountId = createdByAccountId,
                            RecordedAt = DateTime.UtcNow,
                            IsPublished = previousAssessmentResult?.IsPublished ?? false,
                            AttemptNo = 1,
                            PassingScoreSnapshot = assessment.PassingScore,
                            WeightSnapshot = assessment.Weight
                        };
                        await _unitOfWork.AssessmentResultRepository.AddAsync(assessmentResult, ct);
                    }

                    var subjectChecklists = allChecklists.Where(p => p.SubjectId == cs.SubjectId).ToList();
                    foreach (var checklist in subjectChecklists)
                    {
                        var previousChecklistResult = isCarriedOver
                            ? previousChecklistResults.FirstOrDefault(pr => pr.PracticalChecklistId == checklist.PracticalChecklistId && pr.SubjectResultId == previousSubjectResult!.SubjectResultId)
                            : null;

                        var checklistResult = new PracticalChecklistResult
                        {
                            SubjectResultId = subjectResult.SubjectResultId,
                            PracticalChecklistId = checklist.PracticalChecklistId,
                            Score = previousChecklistResult?.Score ?? 0,
                            ResultStatus = previousChecklistResult?.ResultStatus ?? "Pending",
                            IsPublished = previousChecklistResult?.IsPublished ?? false
                        };
                        await _unitOfWork.PracticalChecklistResultRepository.AddAsync(checklistResult, ct);
                    }
                }

        await _unitOfWork.SaveAsync(ct);

        return new CreateEnrollmentResponse(
            enrollment.EnrollmentId,
            enrollment.AccountId,
            enrollment.ClassId,
            enrollment.Status,
            enrollment.EnrolledAt,
            etrRecord.ETRCourseRecordId,
            etrRecord.Status,
            etrRecord.IsLocked);
    }

    public async Task<EnrollmentResponse> UpdateEnrollmentAsync(int id, UpdateEnrollmentRequest request, int updatedByAccountId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInStrategyAsync(async (ct) =>
        {
            await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var item = await _unitOfWork.CourseEnrollmentRepository.GetByIdAsync(id, ct);
                if (item == null) throw new KeyNotFoundException("Enrollment not found.");

                item.AccountId = request.LearnerId;
                item.ClassId = request.ClassId;
                item.Status = request.Status;
                item.EnrolledAt = request.EnrolledAt;
                item.UpdatedAt = DateTime.UtcNow;
                item.UpdatedByAccountId = updatedByAccountId;

                _unitOfWork.CourseEnrollmentRepository.Update(item);

                await _unitOfWork.SaveAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                return new EnrollmentResponse(
                    item.EnrollmentId,
                    item.AccountId,
                    item.ClassId,
                    item.Status,
                    item.EnrolledAt);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }, cancellationToken);
    }

    public async Task DeleteEnrollmentAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default)
    {
        var item = await _unitOfWork.CourseEnrollmentRepository.GetByIdAsync(id, cancellationToken);
        if (item == null) throw new KeyNotFoundException("Enrollment not found.");

        var etrRecord = (await _unitOfWork.ETRCourseRecordRepository.GetAllAsync(cancellationToken))
            .FirstOrDefault(e => e.EnrollmentId == id);

        if (etrRecord != null && (etrRecord.IsLocked || (etrRecord.Status != EtrStatus.Draft && etrRecord.Status != EtrStatus.InProgress)))
        {
            throw new BusinessRuleViolationException($"Cannot delete enrollment because its ETRCourseRecord is already {etrRecord.Status}{(etrRecord.IsLocked ? " and locked" : string.Empty)}.");
        }

        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            AccountId = deletedByAccountId,
            ActionType = AuditActionType.DELETE.ToString(),
            EntityName = nameof(CourseEnrollment),
            RecordId = id,
            OldValue = item.Status.ToString(),
            NewValue = "Deleted",
            Description = $"Enrollment #{id} (Account {item.AccountId}, Class {item.ClassId}) deleted"
        }, cancellationToken);

        item.Status = EnrollmentStatus.Withdrawn;
        item.IsDeleted = true;
        item.DeletedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;
        item.UpdatedByAccountId = deletedByAccountId;

        _unitOfWork.CourseEnrollmentRepository.Update(item);

        // Cascade: an Enrollment cannot be withdrawn while leaving its ETR behind in an "active"
        // state — the guard above only allows this while the ETR is still Draft/InProgress, so
        // cancelling it here is safe (no completed/locked data is being touched). AttendanceRecord
        // now points straight at CourseEnrollment.EnrollmentId (no separate ClassStudent row to keep
        // in sync anymore — mục #10, docs/todo/9.todo_to_complete_system.md).
        if (etrRecord != null)
        {
            etrRecord.Status = EtrStatus.Cancelled;
            etrRecord.IsDeleted = true;
            etrRecord.DeletedAt = DateTime.UtcNow;
            etrRecord.UpdatedAt = DateTime.UtcNow;
            etrRecord.UpdatedByAccountId = deletedByAccountId;
            _unitOfWork.ETRCourseRecordRepository.Update(etrRecord);
        }

        await _unitOfWork.SaveAsync(cancellationToken);
    }
}
