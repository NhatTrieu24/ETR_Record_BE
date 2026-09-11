using ETR.Application.Compliance;
using ETR.Application.DTOs.Department;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using ETR.Domain.Enums;

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
        var existingDepartments = await _unitOfWork.DepartmentRepository.GetAllAsync(cancellationToken);
        if (existingDepartments.Any(d => d.DepartmentName == request.DepartmentName))
        {
            throw new BusinessRuleViolationException($"A department named '{request.DepartmentName}' already exists.");
        }

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

        var existingDepartments = await _unitOfWork.DepartmentRepository.GetAllAsync(cancellationToken);
        if (existingDepartments.Any(d => d.DepartmentId != id && d.DepartmentName == request.DepartmentName))
        {
            throw new BusinessRuleViolationException($"A department named '{request.DepartmentName}' already exists.");
        }

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

        if (id == 1 || id == 2 ||
            department.DepartmentName.Equals("Administration", StringComparison.OrdinalIgnoreCase) ||
            department.DepartmentName.Equals("Training", StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessRuleViolationException("Không thể xóa các phòng ban mặc định cốt lõi của hệ thống (Administration / Training).");
        }

        var hasActiveAccounts = (await _unitOfWork.AccountRepository.GetAllAsync(cancellationToken))
            .Any(a => a.DepartmentId == id && a.IsActive && !a.IsDeleted);
        if (hasActiveAccounts)
        {
            throw new BusinessRuleViolationException($"Không thể xóa phòng ban '{department.DepartmentName}' vì vẫn còn tài khoản người dùng đang trực thuộc. Vui lòng chuyển phòng ban cho nhân sự trước khi xóa.");
        }

        department.IsDeleted = true;
        department.DeletedAt = DateTime.UtcNow;
        department.UpdatedAt = DateTime.UtcNow;
        department.UpdatedByAccountId = deletedByAccountId;

        _unitOfWork.DepartmentRepository.Update(department);

        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            AccountId = deletedByAccountId,
            ActionType = AuditActionType.DELETE.ToString(),
            EntityName = nameof(Department),
            RecordId = department.DepartmentId,
            OldValue = department.DepartmentName,
            NewValue = "Deleted",
            Description = $"Department #{department.DepartmentId} ({department.DepartmentName}) soft-deleted"
        }, cancellationToken);

        await _unitOfWork.SaveAsync(cancellationToken);
    }
}
