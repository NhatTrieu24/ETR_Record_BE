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

    public async Task<CreateEnrollmentResponse> CreateEnrollmentAsync(
        int learnerId,
        int classId,
        int createdByUserId,
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInStrategyAsync(async (ct) =>
        {
            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var trainingClass = await _unitOfWork.ClassRepository.GetByIdAsync(classId, ct);
                if (trainingClass == null) throw new InvalidOperationException("Class not found.");

                var activeEnrollments = await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(ct);
                var activeLearnerEnrollments = activeEnrollments
                    .Where(e => e.LearnerId == learnerId && e.Status == "Active")
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
                    LearnerId = learnerId,
                    ClassId = classId,
                    Status = "Active",
                    EnrolledAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = createdByUserId
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
                    CreatedBy = createdByUserId
                };

                await _unitOfWork.ETRCourseRecordRepository.AddAsync(etrRecord, ct);
                await _unitOfWork.SaveAsync(ct);

                var courseSubjects = (await _unitOfWork.CourseSubjectRepository.GetAllAsync(ct))
                    .Where(cs => cs.CourseId == trainingClass.CourseId).ToList();

                foreach (var cs in courseSubjects)
                {
                    var subjectResult = new SubjectResult
                    {
                        EnrollmentId = enrollment.EnrollmentId,
                        CourseId = cs.CourseId,
                        SubjectId = cs.SubjectId,
                        Status = "Pending",
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = createdByUserId
                    };
                    await _unitOfWork.SubjectResultRepository.AddAsync(subjectResult, ct);
                }

                await _unitOfWork.SaveAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                return new CreateEnrollmentResponse(
                    enrollment.EnrollmentId,
                    enrollment.LearnerId,
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
}
