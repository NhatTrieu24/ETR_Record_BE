using ETR.Application.DTOs.Facility;
using ETR.Application.Interfaces;
using ETR.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// [Module/Flow]: Facility &amp; Training Location Management
/// [Core Responsibility]: Quản lý danh mục cơ sở đào tạo, phòng học, thiết bị mô phỏng, sân bay huấn luyện và kiểm tra tính khả dụng.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Academic,Instructor")]
public class TrainingFacilitiesController : ControllerBase
{
    private readonly ITrainingFacilityService _facilityService;
    private readonly ICurrentUserService _currentUserService;

    public TrainingFacilitiesController(ITrainingFacilityService facilityService, ICurrentUserService currentUserService)
    {
        _facilityService = facilityService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Lấy danh sách tất cả cơ sở đào tạo (có thể lọc theo loại cơ sở và trạng thái hoạt động).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllFacilities(
        [FromQuery] FacilityType? facilityType,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var facilities = await _facilityService.GetAllAsync(facilityType, isActive, cancellationToken);
        return Ok(facilities);
    }

    /// <summary>
    /// Lấy chi tiết thông tin cơ sở đào tạo theo ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetFacilityById(int id, CancellationToken cancellationToken)
    {
        var facility = await _facilityService.GetByIdAsync(id, cancellationToken);
        return Ok(facility);
    }

    /// <summary>
    /// Tra cứu danh sách các cơ sở đào tạo còn trống (không bị trùng lịch) trong khung giờ chỉ định.
    /// </summary>
    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableFacilities(
        [FromQuery] DateTime startAt,
        [FromQuery] DateTime endAt,
        [FromQuery] FacilityType? facilityType,
        [FromQuery] int? excludeSessionId,
        CancellationToken cancellationToken)
    {
        var facilities = await _facilityService.GetAvailableFacilitiesAsync(startAt, endAt, facilityType, excludeSessionId, cancellationToken);
        return Ok(facilities);
    }

    /// <summary>
    /// Thêm mới cơ sở đào tạo (Classroom, Workshop, Simulator, Airfield).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Academic")]
    public async Task<IActionResult> CreateFacility(
        [FromBody] CreateTrainingFacilityRequest request,
        CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var facility = await _facilityService.CreateAsync(request, accountId, cancellationToken);
        return CreatedAtAction(nameof(GetFacilityById), new { id = facility.FacilityId }, facility);
    }

    /// <summary>
    /// Cập nhật thông tin cơ sở đào tạo.
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Academic")]
    public async Task<IActionResult> UpdateFacility(
        int id,
        [FromBody] UpdateTrainingFacilityRequest request,
        CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var facility = await _facilityService.UpdateAsync(id, request, accountId, cancellationToken);
        return Ok(facility);
    }

    /// <summary>
    /// Xóa (soft-delete) cơ sở đào tạo.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Academic")]
    public async Task<IActionResult> DeleteFacility(int id, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        await _facilityService.DeleteAsync(id, accountId, cancellationToken);
        return NoContent();
    }
}
