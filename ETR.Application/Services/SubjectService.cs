using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;

namespace ETR.Application.Services;

public class SubjectService : ISubjectService
{
    private readonly IUnitOfWork _unitOfWork;

    public SubjectService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<SubjectResponse>> GetAllSubjectsAsync(CancellationToken cancellationToken = default)
    {
        var subjects = await _unitOfWork.SubjectRepository.GetAllAsync(cancellationToken);
        return subjects.Where(s => !s.IsDeleted).Select(s => new SubjectResponse(
            s.SubjectId, s.SubjectCode, s.SubjectName, s.SubjectType, s.DefaultHours, s.AssessmentMethod, s.Description, s.Status));
    }

    public async Task<SubjectResponse> GetSubjectByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var s = await _unitOfWork.SubjectRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Subject not found.");

        if (s.IsDeleted) throw new KeyNotFoundException("Subject not found.");

        return new SubjectResponse(s.SubjectId, s.SubjectCode, s.SubjectName, s.SubjectType, s.DefaultHours, s.AssessmentMethod, s.Description, s.Status);
    }

    public async Task<SubjectResponse> CreateSubjectAsync(CreateSubjectRequest request, int createdByAccountId, CancellationToken cancellationToken = default)
    {
        var subject = new Subject
        {
            SubjectCode = request.SubjectCode,
            SubjectName = request.SubjectName,
            SubjectType = request.SubjectType,
            DefaultHours = request.DefaultHours,
            AssessmentMethod = request.AssessmentMethod,
            Description = request.Description,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow,
            CreatedByAccountId = createdByAccountId
        };

        await _unitOfWork.SubjectRepository.AddAsync(subject, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new SubjectResponse(subject.SubjectId, subject.SubjectCode, subject.SubjectName, subject.SubjectType, subject.DefaultHours, subject.AssessmentMethod, subject.Description, subject.Status);
    }

    public async Task<SubjectResponse> UpdateSubjectAsync(int id, UpdateSubjectRequest request, int updatedByAccountId, CancellationToken cancellationToken = default)
    {
        var subject = await _unitOfWork.SubjectRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Subject not found.");

        if (subject.IsDeleted) throw new KeyNotFoundException("Subject not found.");

        subject.SubjectCode = request.SubjectCode;
        subject.SubjectName = request.SubjectName;
        subject.SubjectType = request.SubjectType;
        subject.DefaultHours = request.DefaultHours;
        subject.AssessmentMethod = request.AssessmentMethod;
        subject.Description = request.Description;
        subject.Status = request.Status;
        subject.UpdatedAt = DateTime.UtcNow;
        subject.UpdatedByAccountId = updatedByAccountId;

        _unitOfWork.SubjectRepository.Update(subject);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new SubjectResponse(subject.SubjectId, subject.SubjectCode, subject.SubjectName, subject.SubjectType, subject.DefaultHours, subject.AssessmentMethod, subject.Description, subject.Status);
    }

    public async Task DeleteSubjectAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default)
    {
        var subject = await _unitOfWork.SubjectRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Subject not found.");

        if (subject.IsDeleted) return;

        // Soft Delete
        subject.IsDeleted = true;
        subject.DeletedAt = DateTime.UtcNow;
        subject.UpdatedAt = DateTime.UtcNow;
        subject.UpdatedByAccountId = deletedByAccountId;

        _unitOfWork.SubjectRepository.Update(subject);
        await _unitOfWork.SaveAsync(cancellationToken);
    }
}
