using ETR.Application.DTOs.Department;

namespace ETR.Application.Interfaces;

public interface IDepartmentService
{
    Task<IEnumerable<DepartmentResponse>> GetAllDepartmentsAsync(CancellationToken cancellationToken = default);
    Task<DepartmentResponse> GetDepartmentByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<DepartmentResponse> CreateDepartmentAsync(CreateDepartmentRequest request, int createdByAccountId, CancellationToken cancellationToken = default);
    Task<DepartmentResponse> UpdateDepartmentAsync(int id, UpdateDepartmentRequest request, int updatedByAccountId, CancellationToken cancellationToken = default);
    Task DeleteDepartmentAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default);
}
