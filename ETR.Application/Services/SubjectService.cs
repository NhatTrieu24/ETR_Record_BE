using ETR.Application.Compliance;
using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using ETR.Domain.Enums;

namespace ETR.Application.Services;

public class SubjectService : ISubjectService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICourseService _courseService;

    public SubjectService(IUnitOfWork unitOfWork, ICourseService courseService)
    {
        _unitOfWork = unitOfWork;
        _courseService = courseService;
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
        var codeExists = _unitOfWork.SubjectRepository.GetQueryable()
            .Any(s => !s.IsDeleted && s.SubjectCode == request.SubjectCode);
        if (codeExists)
        {
            throw new BusinessRuleViolationException($"A subject with code '{request.SubjectCode}' already exists.");
        }

        var subject = new Subject
        {
            SubjectCode = request.SubjectCode,
            SubjectName = request.SubjectName,
            SubjectType = request.SubjectType,
            DefaultHours = request.DefaultHours,
            AssessmentMethod = request.AssessmentMethod,
            Description = request.Description,
            MinSessions = request.MinSessions,
            MaxSessions = request.MaxSessions,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow,
            CreatedByAccountId = createdByAccountId
        };

        await _unitOfWork.SubjectRepository.AddAsync(subject, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            AccountId = createdByAccountId,
            ActionType = AuditActionType.INSERT.ToString(),
            EntityName = nameof(Subject),
            RecordId = subject.SubjectId,
            NewValue = $"{subject.SubjectCode} - {subject.SubjectName}",
            Description = $"Created Subject #{subject.SubjectId} ({subject.SubjectCode} - {subject.SubjectName})"
        }, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new SubjectResponse(subject.SubjectId, subject.SubjectCode, subject.SubjectName, subject.SubjectType, subject.DefaultHours, subject.AssessmentMethod, subject.Description, subject.Status);
    }

    public async Task<SubjectResponse> UpdateSubjectAsync(int id, UpdateSubjectRequest request, int updatedByAccountId, CancellationToken cancellationToken = default)
    {
        var subject = await _unitOfWork.SubjectRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Subject not found.");

        if (subject.IsDeleted) throw new KeyNotFoundException("Subject not found.");

        bool isCoreFieldChanged = subject.SubjectCode != request.SubjectCode
            || subject.SubjectName != request.SubjectName
            || subject.SubjectType != request.SubjectType
            || subject.DefaultHours != request.DefaultHours
            || subject.AssessmentMethod != request.AssessmentMethod
            || subject.MinSessions != request.MinSessions
            || subject.MaxSessions != request.MaxSessions
            || subject.Status != request.Status;

        if (isCoreFieldChanged)
        {
            var courseIds = (await _unitOfWork.CourseSubjectRepository.GetAllAsync(cancellationToken))
                .Where(cs => cs.SubjectId == id && !cs.IsDeleted)
                .Select(cs => cs.CourseId)
                .Distinct()
                .ToList();

            foreach (var cId in courseIds)
            {
                await _courseService.EnsureCourseNotLockedAsync(cId, cancellationToken);
            }
        }

        var codeExists = _unitOfWork.SubjectRepository.GetQueryable()
            .Any(s => s.SubjectId != id && !s.IsDeleted && s.SubjectCode == request.SubjectCode);
        if (codeExists)
        {
            throw new BusinessRuleViolationException($"A subject with code '{request.SubjectCode}' already exists.");
        }

        var oldCode = subject.SubjectCode;
        var oldName = subject.SubjectName;

        subject.SubjectCode = request.SubjectCode;
        subject.SubjectName = request.SubjectName;
        subject.SubjectType = request.SubjectType;
        subject.DefaultHours = request.DefaultHours;
        subject.AssessmentMethod = request.AssessmentMethod;
        subject.Description = request.Description;
        subject.MinSessions = request.MinSessions;
        subject.MaxSessions = request.MaxSessions;
        subject.Status = request.Status;
        subject.UpdatedAt = DateTime.UtcNow;
        subject.UpdatedByAccountId = updatedByAccountId;

        _unitOfWork.SubjectRepository.Update(subject);

        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            AccountId = updatedByAccountId,
            ActionType = AuditActionType.UPDATE.ToString(),
            EntityName = nameof(Subject),
            RecordId = subject.SubjectId,
            OldValue = $"{oldCode} - {oldName}",
            NewValue = $"{subject.SubjectCode} - {subject.SubjectName}",
            Description = $"Updated Subject #{subject.SubjectId} ({subject.SubjectCode} - {subject.SubjectName})"
        }, cancellationToken);

        await _unitOfWork.SaveAsync(cancellationToken);

        return new SubjectResponse(subject.SubjectId, subject.SubjectCode, subject.SubjectName, subject.SubjectType, subject.DefaultHours, subject.AssessmentMethod, subject.Description, subject.Status);
    }

    public async Task DeleteSubjectAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default)
    {
        var subject = await _unitOfWork.SubjectRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Subject not found.");

        if (subject.IsDeleted) return;

        var isUsedInCourse = (await _unitOfWork.CourseSubjectRepository.GetAllAsync(cancellationToken))
            .Any(cs => cs.SubjectId == id && !cs.IsDeleted);
        if (isUsedInCourse)
        {
            throw new BusinessRuleViolationException($"Không thể xóa môn học '{subject.SubjectName}' vì đang được liên kết trong khóa học đào tạo. Vui lòng gỡ môn học khỏi các khóa học trước.");
        }

        var isUsedInClass = (await _unitOfWork.ClassSubjectRepository.GetAllAsync(cancellationToken))
            .Any(cs => cs.SubjectId == id && !cs.IsDeleted);
        if (isUsedInClass)
        {
            throw new BusinessRuleViolationException($"Không thể xóa môn học '{subject.SubjectName}' vì đang được mở giảng dạy trong lớp học.");
        }

        // Soft Delete
        subject.IsDeleted = true;
        subject.DeletedAt = DateTime.UtcNow;
        subject.UpdatedAt = DateTime.UtcNow;
        subject.UpdatedByAccountId = deletedByAccountId;

        _unitOfWork.SubjectRepository.Update(subject);

        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            AccountId = deletedByAccountId,
            ActionType = AuditActionType.DELETE.ToString(),
            EntityName = nameof(Subject),
            RecordId = subject.SubjectId,
            OldValue = subject.SubjectName,
            NewValue = "Deleted",
            Description = $"Subject #{subject.SubjectId} ({subject.SubjectCode} - {subject.SubjectName}) soft-deleted"
        }, cancellationToken);

        await _unitOfWork.SaveAsync(cancellationToken);
    }
}
