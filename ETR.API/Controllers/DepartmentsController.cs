using ETR.Application.DTOs.Department;
using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departmentService;
    private readonly ICurrentUserService _currentUserService;

    public DepartmentsController(IDepartmentService departmentService, ICurrentUserService currentUserService)
    {
        _departmentService = departmentService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Academic")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _departmentService.GetAllDepartmentsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Academic")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _departmentService.GetDepartmentByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var result = await _departmentService.CreateDepartmentAsync(request, accountId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.DepartmentId }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDepartmentRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var result = await _departmentService.UpdateDepartmentAsync(id, request, accountId, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        await _departmentService.DeleteDepartmentAsync(id, accountId, cancellationToken);
        return NoContent();
    }
}
