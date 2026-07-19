using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;

namespace ETR.Application.Services;

public class ClassService : IClassService
{
    private readonly IUnitOfWork _unitOfWork;

    public ClassService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<TrainingClassResponse>> GetAllClassesAsync(CancellationToken cancellationToken = default)
    {
        var classes = await _unitOfWork.ClassRepository.GetAllAsync(cancellationToken);
        return classes.Where(c => !c.IsDeleted).Select(c => new TrainingClassResponse(
            c.ClassId, c.ClassCode, c.ClassName, c.CourseId, c.StartDate, c.EndDate, c.Location, c.Capacity, c.Status));
    }

    public async Task<TrainingClassResponse> GetClassByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var c = await _unitOfWork.ClassRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Class not found.");

        if (c.IsDeleted) throw new KeyNotFoundException("Class not found.");

        return new TrainingClassResponse(c.ClassId, c.ClassCode, c.ClassName, c.CourseId, c.StartDate, c.EndDate, c.Location, c.Capacity, c.Status);
    }

    public async Task<TrainingClassResponse> CreateClassAsync(CreateClassRequest request, int createdByAccountId, CancellationToken cancellationToken = default)
    {
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
            CreatedAt = DateTime.UtcNow,
            CreatedByAccountId = createdByAccountId
        };

        await _unitOfWork.ClassRepository.AddAsync(cls, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new TrainingClassResponse(cls.ClassId, cls.ClassCode, cls.ClassName, cls.CourseId, cls.StartDate, cls.EndDate, cls.Location, cls.Capacity, cls.Status);
    }

    public async Task<TrainingClassResponse> UpdateClassAsync(int id, UpdateClassRequest request, int updatedByAccountId, CancellationToken cancellationToken = default)
    {
        var cls = await _unitOfWork.ClassRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Class not found.");

        if (cls.IsDeleted) throw new KeyNotFoundException("Class not found.");

        cls.ClassCode = request.ClassCode;
        cls.ClassName = request.ClassName;
        cls.CourseId = request.CourseId;
        cls.StartDate = request.StartDate;
        cls.EndDate = request.EndDate;
        cls.Location = request.Location;
        cls.Capacity = request.Capacity;
        cls.Status = request.Status;
        cls.UpdatedAt = DateTime.UtcNow;
        cls.UpdatedByAccountId = updatedByAccountId;

        _unitOfWork.ClassRepository.Update(cls);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new TrainingClassResponse(cls.ClassId, cls.ClassCode, cls.ClassName, cls.CourseId, cls.StartDate, cls.EndDate, cls.Location, cls.Capacity, cls.Status);
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
        await _unitOfWork.SaveAsync(cancellationToken);
    }
}
