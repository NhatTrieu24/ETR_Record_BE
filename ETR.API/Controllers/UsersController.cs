using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

[ApiController]
[Route("api")]
public class UsersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public UsersController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    #region User Management

    [HttpGet("users")]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetUsers(CancellationToken cancellationToken)
    {
        var list = await _unitOfWork.UserRepository.GetAllAsync(cancellationToken);
        return Ok(list.Select(MapUserToResponse));
    }

    [HttpGet("users/{id:int}")]
    public async Task<ActionResult<UserResponse>> GetUserById(int id, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.UserRepository.GetByIdAsync(id, cancellationToken);
        if (user == null) return NotFound($"Không tìm thấy người dùng với ID {id}.");
        return Ok(MapUserToResponse(user));
    }

    [HttpPost("users")]
    public async Task<ActionResult<UserResponse>> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var user = new User
        {
            Username = request.Username,
            PasswordHash = request.PasswordHash,
            FullName = request.FullName,
            Email = request.Email,
            RoleId = request.RoleId,
            DepartmentId = request.DepartmentId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _unitOfWork.UserRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return CreatedAtAction(nameof(GetUserById), new { id = user.UserId }, MapUserToResponse(user));
    }

    [HttpPut("users/{id:int}")]
    public async Task<ActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        if (id != request.UserId) return BadRequest("ID không khớp.");

        var existingUser = await _unitOfWork.UserRepository.GetByIdAsync(id, cancellationToken);
        if (existingUser == null) return NotFound($"Không tìm thấy người dùng với ID {id}.");

        existingUser.FullName = request.FullName;
        existingUser.Email = request.Email;
        existingUser.RoleId = request.RoleId;
        existingUser.DepartmentId = request.DepartmentId;
        existingUser.IsActive = request.IsActive;
        existingUser.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.UserRepository.Update(existingUser);
        await _unitOfWork.SaveAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("users/{id:int}")]
    public async Task<ActionResult> DeleteUser(int id, CancellationToken cancellationToken)
    {
        var existingUser = await _unitOfWork.UserRepository.GetByIdAsync(id, cancellationToken);
        if (existingUser == null) return NotFound($"Không tìm thấy người dùng với ID {id}.");

        _unitOfWork.UserRepository.Delete(existingUser);
        await _unitOfWork.SaveAsync(cancellationToken);

        return NoContent();
    }

    [HttpPatch("users/{id:int}/activate")]
    public async Task<ActionResult<UserResponse>> ActivateUser(int id, CancellationToken cancellationToken)
    {
        var existingUser = await _unitOfWork.UserRepository.GetByIdAsync(id, cancellationToken);
        if (existingUser == null) return NotFound($"Không tìm thấy người dùng với ID {id}.");

        existingUser.IsActive = true;
        existingUser.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.UserRepository.Update(existingUser);
        await _unitOfWork.SaveAsync(cancellationToken);

        return Ok(MapUserToResponse(existingUser));
    }

    [HttpPatch("users/{id:int}/deactivate")]
    public async Task<ActionResult<UserResponse>> DeactivateUser(int id, CancellationToken cancellationToken)
    {
        var existingUser = await _unitOfWork.UserRepository.GetByIdAsync(id, cancellationToken);
        if (existingUser == null) return NotFound($"Không tìm thấy người dùng với ID {id}.");

        existingUser.IsActive = false;
        existingUser.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.UserRepository.Update(existingUser);
        await _unitOfWork.SaveAsync(cancellationToken);

        return Ok(MapUserToResponse(existingUser));
    }

    [HttpGet("users/search")]
    public async Task<ActionResult<IEnumerable<UserResponse>>> SearchUsers([FromQuery] string query, CancellationToken cancellationToken)
    {
        var users = await _unitOfWork.UserRepository.GetAllAsync(cancellationToken);
        var filtered = users.Where(u => u.Username.Contains(query, StringComparison.OrdinalIgnoreCase) || 
                                        u.FullName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                        u.Email.Contains(query, StringComparison.OrdinalIgnoreCase));
        return Ok(filtered.Select(MapUserToResponse));
    }

    #endregion

    #region Role Management

    [HttpGet("roles")]
    public async Task<ActionResult<IEnumerable<RoleResponse>>> GetRoles(CancellationToken cancellationToken)
    {
        var list = await _unitOfWork.RoleRepository.GetAllAsync(cancellationToken);
        return Ok(list.Select(MapRoleToResponse));
    }

    [HttpGet("roles/{id:int}")]
    public async Task<ActionResult<RoleResponse>> GetRoleById(int id, CancellationToken cancellationToken)
    {
        var role = await _unitOfWork.RoleRepository.GetByIdAsync(id, cancellationToken);
        if (role == null) return NotFound($"Không tìm thấy vai trò với ID {id}.");
        return Ok(MapRoleToResponse(role));
    }

    [HttpPost("roles")]
    public async Task<ActionResult<RoleResponse>> CreateRole([FromBody] CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var role = new Role
        {
            RoleName = request.RoleName,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _unitOfWork.RoleRepository.AddAsync(role, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return CreatedAtAction(nameof(GetRoleById), new { id = role.RoleId }, MapRoleToResponse(role));
    }

    [HttpPut("roles/{id:int}")]
    public async Task<ActionResult> UpdateRole(int id, [FromBody] UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        if (id != request.RoleId) return BadRequest("ID không khớp.");

        var existingRole = await _unitOfWork.RoleRepository.GetByIdAsync(id, cancellationToken);
        if (existingRole == null) return NotFound($"Không tìm thấy vai trò với ID {id}.");

        existingRole.RoleName = request.RoleName;
        existingRole.Description = request.Description;
        existingRole.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.RoleRepository.Update(existingRole);
        await _unitOfWork.SaveAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("roles/{id:int}")]
    public async Task<ActionResult> DeleteRole(int id, CancellationToken cancellationToken)
    {
        var existingRole = await _unitOfWork.RoleRepository.GetByIdAsync(id, cancellationToken);
        if (existingRole == null) return NotFound($"Không tìm thấy vai trò với ID {id}.");

        _unitOfWork.RoleRepository.Delete(existingRole);
        await _unitOfWork.SaveAsync(cancellationToken);

        return NoContent();
    }

    #endregion

    #region Department Management

    [HttpGet("departments")]
    public async Task<ActionResult<IEnumerable<DepartmentResponse>>> GetDepartments(CancellationToken cancellationToken)
    {
        var list = await _unitOfWork.DepartmentRepository.GetAllAsync(cancellationToken);
        return Ok(list.Select(MapDeptToResponse));
    }

    [HttpGet("departments/{id:int}")]
    public async Task<ActionResult<DepartmentResponse>> GetDepartmentById(int id, CancellationToken cancellationToken)
    {
        var dept = await _unitOfWork.DepartmentRepository.GetByIdAsync(id, cancellationToken);
        if (dept == null) return NotFound($"Không tìm thấy phòng ban với ID {id}.");
        return Ok(MapDeptToResponse(dept));
    }

    [HttpPost("departments")]
    public async Task<ActionResult<DepartmentResponse>> CreateDepartment([FromBody] CreateDepartmentRequest request, CancellationToken cancellationToken)
    {
        var dept = new Department
        {
            DepartmentName = request.DepartmentName,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _unitOfWork.DepartmentRepository.AddAsync(dept, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return CreatedAtAction(nameof(GetDepartmentById), new { id = dept.DepartmentId }, MapDeptToResponse(dept));
    }

    [HttpPut("departments/{id:int}")]
    public async Task<ActionResult> UpdateDepartment(int id, [FromBody] UpdateDepartmentRequest request, CancellationToken cancellationToken)
    {
        if (id != request.DepartmentId) return BadRequest("ID không khớp.");

        var existingDept = await _unitOfWork.DepartmentRepository.GetByIdAsync(id, cancellationToken);
        if (existingDept == null) return NotFound($"Không tìm thấy phòng ban với ID {id}.");

        existingDept.DepartmentName = request.DepartmentName;
        existingDept.Description = request.Description;
        existingDept.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.DepartmentRepository.Update(existingDept);
        await _unitOfWork.SaveAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("departments/{id:int}")]
    public async Task<ActionResult> DeleteDepartment(int id, CancellationToken cancellationToken)
    {
        var existingDept = await _unitOfWork.DepartmentRepository.GetByIdAsync(id, cancellationToken);
        if (existingDept == null) return NotFound($"Không tìm thấy phòng ban với ID {id}.");

        _unitOfWork.DepartmentRepository.Delete(existingDept);
        await _unitOfWork.SaveAsync(cancellationToken);

        return NoContent();
    }

    #endregion

    #region Learner Type Management

    [HttpGet("learner-types")]
    public async Task<ActionResult<IEnumerable<LearnerTypeResponse>>> GetLearnerTypes(CancellationToken cancellationToken)
    {
        var list = await _unitOfWork.LearnerTypeRepository.GetAllAsync(cancellationToken);
        return Ok(list.Select(MapTypeToResponse));
    }

    [HttpGet("learner-types/{id:int}")]
    public async Task<ActionResult<LearnerTypeResponse>> GetLearnerTypeById(int id, CancellationToken cancellationToken)
    {
        var type = await _unitOfWork.LearnerTypeRepository.GetByIdAsync(id, cancellationToken);
        if (type == null) return NotFound($"Không tìm thấy loại học viên với ID {id}.");
        return Ok(MapTypeToResponse(type));
    }

    [HttpPost("learner-types")]
    public async Task<ActionResult<LearnerTypeResponse>> CreateLearnerType([FromBody] CreateLearnerTypeRequest request, CancellationToken cancellationToken)
    {
        var type = new LearnerType
        {
            TypeName = request.TypeName,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _unitOfWork.LearnerTypeRepository.AddAsync(type, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return CreatedAtAction(nameof(GetLearnerTypeById), new { id = type.LearnerTypeId }, MapTypeToResponse(type));
    }

    [HttpPut("learner-types/{id:int}")]
    public async Task<ActionResult> UpdateLearnerType(int id, [FromBody] UpdateLearnerTypeRequest request, CancellationToken cancellationToken)
    {
        if (id != request.LearnerTypeId) return BadRequest("ID không khớp.");

        var existingType = await _unitOfWork.LearnerTypeRepository.GetByIdAsync(id, cancellationToken);
        if (existingType == null) return NotFound($"Không tìm thấy loại học viên với ID {id}.");

        existingType.TypeName = request.TypeName;
        existingType.Description = request.Description;
        existingType.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.LearnerTypeRepository.Update(existingType);
        await _unitOfWork.SaveAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("learner-types/{id:int}")]
    public async Task<ActionResult> DeleteLearnerType(int id, CancellationToken cancellationToken)
    {
        var existingType = await _unitOfWork.LearnerTypeRepository.GetByIdAsync(id, cancellationToken);
        if (existingType == null) return NotFound($"Không tìm thấy loại học viên với ID {id}.");

        _unitOfWork.LearnerTypeRepository.Delete(existingType);
        await _unitOfWork.SaveAsync(cancellationToken);

        return NoContent();
    }

    #endregion

    private static UserResponse MapUserToResponse(User u)
    {
        return new UserResponse(u.UserId, u.Username, u.FullName, u.Email, u.RoleId, u.DepartmentId, u.IsActive);
    }

    private static RoleResponse MapRoleToResponse(Role r)
    {
        return new RoleResponse(r.RoleId, r.RoleName, r.Description);
    }

    private static DepartmentResponse MapDeptToResponse(Department d)
    {
        return new DepartmentResponse(d.DepartmentId, d.DepartmentName, d.Description);
    }

    private static LearnerTypeResponse MapTypeToResponse(LearnerType t)
    {
        return new LearnerTypeResponse(t.LearnerTypeId, t.TypeName, t.Description);
    }
}
