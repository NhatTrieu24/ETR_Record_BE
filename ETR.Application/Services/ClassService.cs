using ETR.Application.Compliance;
using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;

namespace ETR.Application.Services;

public class ClassService : IClassService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public ClassService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<IEnumerable<TrainingClassResponse>> GetAllClassesAsync(CancellationToken cancellationToken = default)
    {
        var classes = await _unitOfWork.ClassRepository.GetAllAsync(cancellationToken);
        var visible = classes.Where(c => !c.IsDeleted);

        // "Sân nhà ai nấy đá" (team decision 2026-08-08, docs/todo/addition.md): Instructor only
        // sees classes they are actually assigned to, not the whole system's class list.
        if (string.Equals(_currentUserService.RoleName, "Instructor", StringComparison.OrdinalIgnoreCase) && _currentUserService.AccountId.HasValue)
        {
            visible = visible.Where(c => c.InstructorAccountId == _currentUserService.AccountId.Value);
        }

        return visible.Select(c => new TrainingClassResponse(
            c.ClassId, c.ClassCode, c.ClassName, c.CourseId, c.StartDate, c.EndDate, c.Location, c.Capacity, c.Status, c.InstructorAccountId));
    }

    public async Task<TrainingClassResponse> GetClassByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var c = await _unitOfWork.ClassRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Class not found.");

        if (c.IsDeleted) throw new KeyNotFoundException("Class not found.");

        return new TrainingClassResponse(c.ClassId, c.ClassCode, c.ClassName, c.CourseId, c.StartDate, c.EndDate, c.Location, c.Capacity, c.Status, c.InstructorAccountId);
    }

    public async Task<TrainingClassResponse> CreateClassAsync(CreateClassRequest request, int createdByAccountId, CancellationToken cancellationToken = default)
    {
        if (request.InstructorAccountId.HasValue)
        {
            await EnsureAccountHasInstructorRoleAsync(request.InstructorAccountId.Value, cancellationToken);
        }

        var cls = new Class
        {
            ClassCode = request.ClassCode,
            ClassName = request.ClassName,
            CourseId = request.CourseId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Location = request.Location,
            Capacity = request.Capacity,
            Status = request.Status,
            InstructorAccountId = request.InstructorAccountId,
            CreatedAt = DateTime.UtcNow,
            CreatedByAccountId = createdByAccountId
        };

        await _unitOfWork.ClassRepository.AddAsync(cls, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            AccountId = createdByAccountId,
            ActionType = "INSERT",
            EntityName = nameof(Class),
            RecordId = cls.ClassId,
            NewValue = cls.ClassCode,
            Description = $"Class #{cls.ClassId} ({cls.ClassCode}) created"
        }, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new TrainingClassResponse(cls.ClassId, cls.ClassCode, cls.ClassName, cls.CourseId, cls.StartDate, cls.EndDate, cls.Location, cls.Capacity, cls.Status, cls.InstructorAccountId);
    }

    public async Task<TrainingClassResponse> UpdateClassAsync(int id, UpdateClassRequest request, int updatedByAccountId, CancellationToken cancellationToken = default)
    {
        var cls = await _unitOfWork.ClassRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Class not found.");

        if (cls.IsDeleted) throw new KeyNotFoundException("Class not found.");

        if (request.InstructorAccountId.HasValue && request.InstructorAccountId != cls.InstructorAccountId)
        {
            await EnsureAccountHasInstructorRoleAsync(request.InstructorAccountId.Value, cancellationToken);
        }

        var oldStatus = cls.Status;

        cls.ClassCode = request.ClassCode;
        cls.ClassName = request.ClassName;
        cls.CourseId = request.CourseId;
        cls.StartDate = request.StartDate;
        cls.EndDate = request.EndDate;
        cls.Location = request.Location;
        cls.Capacity = request.Capacity;
        cls.Status = request.Status;
        cls.InstructorAccountId = request.InstructorAccountId;
        cls.UpdatedAt = DateTime.UtcNow;
        cls.UpdatedByAccountId = updatedByAccountId;

        _unitOfWork.ClassRepository.Update(cls);

        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            AccountId = updatedByAccountId,
            ActionType = "UPDATE",
            EntityName = nameof(Class),
            RecordId = cls.ClassId,
            OldValue = oldStatus,
            NewValue = cls.Status,
            Description = $"Class #{cls.ClassId} ({cls.ClassCode}) updated"
        }, cancellationToken);

        await _unitOfWork.SaveAsync(cancellationToken);

        return new TrainingClassResponse(cls.ClassId, cls.ClassCode, cls.ClassName, cls.CourseId, cls.StartDate, cls.EndDate, cls.Location, cls.Capacity, cls.Status, cls.InstructorAccountId);
    }

    public async Task DeleteClassAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default)
    {
        var cls = await _unitOfWork.ClassRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Class not found.");

        if (cls.IsDeleted) return;

        // Soft Delete
        cls.IsDeleted = true;
        cls.DeletedAt = DateTime.UtcNow;
        cls.UpdatedAt = DateTime.UtcNow;
        cls.UpdatedByAccountId = deletedByAccountId;

        _unitOfWork.ClassRepository.Update(cls);

        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            AccountId = deletedByAccountId,
            ActionType = "DELETE",
            EntityName = nameof(Class),
            RecordId = cls.ClassId,
            OldValue = cls.Status,
            NewValue = "Deleted",
            Description = $"Class #{cls.ClassId} ({cls.ClassCode}) deleted"
        }, cancellationToken);

        await _unitOfWork.SaveAsync(cancellationToken);
    }

    private async Task EnsureAccountHasInstructorRoleAsync(int instructorAccountId, CancellationToken cancellationToken)
    {
        var account = await _unitOfWork.AccountRepository.GetByIdAsync(instructorAccountId, cancellationToken)
            ?? throw new BusinessRuleViolationException($"Account (ID: {instructorAccountId}) not found.");

        var role = await _unitOfWork.RoleRepository.GetByIdAsync(account.RoleId, cancellationToken);
        if (role == null || role.RoleName != "Instructor")
        {
            throw new BusinessRuleViolationException("InstructorAccountId must reference an account with the Instructor role.");
        }
    }
}
