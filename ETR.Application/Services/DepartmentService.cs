using ETR.Application.DTOs.Department;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;

namespace ETR.Application.Services;

public class DepartmentService : IDepartmentService
{
    private readonly IUnitOfWork _unitOfWork;

    public DepartmentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<DepartmentResponse>> GetAllDepartmentsAsync(CancellationToken cancellationToken = default)
    {
        var departments = await _unitOfWork.DepartmentRepository.GetAllAsync(cancellationToken);
        return departments.Select(d => new DepartmentResponse
        {
            DepartmentId = d.DepartmentId,
            DepartmentName = d.DepartmentName,
            Description = d.Description
        });
    }

    public async Task<DepartmentResponse> GetDepartmentByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var d = await _unitOfWork.DepartmentRepository.GetByIdAsync(id, cancellationToken);
        if (d == null) throw new KeyNotFoundException("Department not found.");

        return new DepartmentResponse
        {
            DepartmentId = d.DepartmentId,
            DepartmentName = d.DepartmentName,
            Description = d.Description
        };
    }

    public async Task<DepartmentResponse> CreateDepartmentAsync(CreateDepartmentRequest request, int createdByAccountId, CancellationToken cancellationToken = default)
    {
        var department = new Department
        {
            DepartmentName = request.DepartmentName,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            CreatedByAccountId = createdByAccountId
        };

        await _unitOfWork.DepartmentRepository.AddAsync(department, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DepartmentResponse
        {
            DepartmentId = department.DepartmentId,
            DepartmentName = department.DepartmentName,
            Description = department.Description
        };
    }

    public async Task<DepartmentResponse> UpdateDepartmentAsync(int id, UpdateDepartmentRequest request, int updatedByAccountId, CancellationToken cancellationToken = default)
    {
        var department = await _unitOfWork.DepartmentRepository.GetByIdAsync(id, cancellationToken);
        if (department == null) throw new KeyNotFoundException("Department not found.");

        department.DepartmentName = request.DepartmentName;
        department.Description = request.Description;
        department.UpdatedAt = DateTime.UtcNow;
        department.UpdatedByAccountId = updatedByAccountId;

        _unitOfWork.DepartmentRepository.Update(department);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new DepartmentResponse
        {
            DepartmentId = department.DepartmentId,
            DepartmentName = department.DepartmentName,
            Description = department.Description
        };
    }

    public async Task DeleteDepartmentAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default)
    {
        var department = await _unitOfWork.DepartmentRepository.GetByIdAsync(id, cancellationToken);
        if (department == null) throw new KeyNotFoundException("Department not found.");

        department.IsDeleted = true;
        department.DeletedAt = DateTime.UtcNow;
        department.UpdatedAt = DateTime.UtcNow;
        department.UpdatedByAccountId = deletedByAccountId;

        _unitOfWork.DepartmentRepository.Update(department);
        await _unitOfWork.SaveAsync(cancellationToken);
    }
}
