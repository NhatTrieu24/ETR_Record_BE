using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;

namespace ETR.Application.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly IUnitOfWork _unitOfWork;

    public EnrollmentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<EnrollmentResponse>> GetAllEnrollmentsAsync(CancellationToken cancellationToken = default)
    {
        var enrollments = await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(cancellationToken);
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
                var trainingClass = await _unitOfWork.ClassRepository.GetByIdAsync(classId, ct);
                if (trainingClass == null) throw new InvalidOperationException("Class not found.");

                // === BUSINESS RULE 1: Course must have at least one Subject before enrollment ===
                var hasSubjectsFromCheck = (await _unitOfWork.CourseSubjectRepository.GetAllAsync(ct)).Any(cs => cs.CourseId == trainingClass.CourseId);
                if (!hasSubjectsFromCheck)
                {
                    throw new InvalidOperationException($"Cannot enroll. Course (ID: {trainingClass.CourseId}) has no subjects configured. Please add subjects to the course first.");
                }

                var activeEnrollments = await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(ct);
                var activeLearnerEnrollments = activeEnrollments
                    .Where(e => e.AccountId == accountId && e.Status == "Active")
                    .Select(e => e.ClassId)
                    .ToList();
                
                if (activeLearnerEnrollments.Count > 0)
                {
                    var classes = await _unitOfWork.ClassRepository.GetAllAsync(ct);
                    var activeCourseIds = classes
                        .Where(c => activeLearnerEnrollments.Contains(c.ClassId))
                        .Select(c => c.CourseId)
                        .ToList();

                    if (activeCourseIds.Contains(trainingClass.CourseId))
                    {
                        throw new InvalidOperationException("Learner is already enrolled in an active class for this course.");
                    }
                }

                var enrollment = new CourseEnrollment
                {
                    AccountId = accountId,
                    ClassId = classId,
                    Status = "Active",
                    EnrolledAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByAccountId = createdByAccountId
                };

                await _unitOfWork.CourseEnrollmentRepository.AddAsync(enrollment, ct);
                await _unitOfWork.SaveAsync(ct);

                var etrRecord = new ETRCourseRecord
                {
                    EnrollmentId = enrollment.EnrollmentId,
                    Status = "InProgress",
                    IsLocked = false,
                    CreatedBySystem = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByAccountId = createdByAccountId
                };

                await _unitOfWork.ETRCourseRecordRepository.AddAsync(etrRecord, ct);
                await _unitOfWork.SaveAsync(ct);

                var classStudent = new ClassStudent
                {
                    CourseEnrollmentId = enrollment.EnrollmentId,
                    ClassId = classId,
                    AccountId = accountId,
                    Status = "Active"
                };
                
                await _unitOfWork.ClassStudentRepository.AddAsync(classStudent, ct);
                await _unitOfWork.SaveAsync(ct);

                var courseSubjects = (await _unitOfWork.CourseSubjectRepository.GetAllAsync(ct))
                    .Where(cs => cs.CourseId == trainingClass.CourseId).ToList();

                foreach (var cs in courseSubjects)
                {
                    var subjectResult = new SubjectResult
                    {
                        EtrId = etrRecord.ETRCourseRecordId,
                        CourseId = cs.CourseId,
                        SubjectId = cs.SubjectId,
                        Status = "Pending",
                        CreatedAt = DateTime.UtcNow,
                        CreatedByAccountId = accountId
                    };
                    await _unitOfWork.SubjectResultRepository.AddAsync(subjectResult, ct);
                }

                await _unitOfWork.SaveAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

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
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }, cancellationToken);
    }

    public async Task<EnrollmentResponse> UpdateEnrollmentAsync(int id, UpdateEnrollmentRequest request, int updatedByAccountId, CancellationToken cancellationToken = default)
    {
        var item = await _unitOfWork.CourseEnrollmentRepository.GetByIdAsync(id, cancellationToken);
        if (item == null) throw new KeyNotFoundException("Enrollment not found.");

        item.AccountId = request.LearnerId;
        item.ClassId = request.ClassId;
        item.Status = request.Status;
        item.EnrolledAt = request.EnrolledAt;
        item.UpdatedAt = DateTime.UtcNow;
        item.UpdatedByAccountId = updatedByAccountId;

        _unitOfWork.CourseEnrollmentRepository.Update(item);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new EnrollmentResponse(
            item.EnrollmentId,
            item.AccountId,
            item.ClassId,
            item.Status,
            item.EnrolledAt);
    }

    public async Task DeleteEnrollmentAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default)
    {
        var item = await _unitOfWork.CourseEnrollmentRepository.GetByIdAsync(id, cancellationToken);
        if (item == null) throw new KeyNotFoundException("Enrollment not found.");

        item.IsDeleted = true;
        item.DeletedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;
        item.UpdatedByAccountId = deletedByAccountId;

        _unitOfWork.CourseEnrollmentRepository.Update(item);
        await _unitOfWork.SaveAsync(cancellationToken);
    }
}
