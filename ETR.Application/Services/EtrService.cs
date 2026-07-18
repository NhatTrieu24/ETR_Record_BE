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

    public async Task<EtrRecordResponse> SubmitEtrAsync(int etrCourseRecordId, int accountId, CancellationToken cancellationToken = default)
    {
        var etr = await _unitOfWork.ETRCourseRecordRepository.GetByIdAsync(etrCourseRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"ETRCourseRecord not found.");

        if (etr.IsLocked) throw new InvalidOperationException("ETR is locked.");
        
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

        etr.Status = "Verified";
        etr.VerifiedAt = DateTime.UtcNow;
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

        var enrollment = await _unitOfWork.CourseEnrollmentRepository.GetByIdAsync(etr.EnrollmentId, cancellationToken);
        if (enrollment == null) throw new InvalidOperationException("Enrollment not found.");

        var trainingClass = await _unitOfWork.ClassRepository.GetByIdAsync(enrollment.ClassId, cancellationToken);
        if (trainingClass == null) throw new InvalidOperationException("Class not found.");

        var courseSubjects = (await _unitOfWork.CourseSubjectRepository.GetAllAsync(cancellationToken))
            .Where(cs => cs.CourseId == trainingClass.CourseId && cs.IsMandatory).ToList();

        foreach (var cs in courseSubjects)
        {
            var sr = etr.SubjectResults.FirstOrDefault(s => s.SubjectId == cs.SubjectId);
            if (sr == null || (sr.Status != "Passed" && sr.Status != "Exempted"))
            {
                throw new InvalidOperationException($"Cannot complete ETR. Mandatory subject (ID: {cs.SubjectId}) is not Passed or Exempted.");
            }
        }

        etr.Status = "Completed";
        etr.CompletedAt = DateTime.UtcNow;
        etr.IsLocked = true;
        etr.UpdatedAt = DateTime.UtcNow;
        etr.UpdatedByAccountId = accountId;

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