using System.ComponentModel.DataAnnotations;
using ETR.Application.DTOs.Facility;
using ETR.Application.Exceptions;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using ETR.Domain.Enums;

namespace ETR.Application.Services;

public class TrainingFacilityService : ITrainingFacilityService
{
    private readonly IUnitOfWork _unitOfWork;

    public TrainingFacilityService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<TrainingFacilityResponse>> GetAllAsync(
        FacilityType? type = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var facilities = await _unitOfWork.TrainingFacilityRepository.GetAllAsync(cancellationToken);
        var query = facilities.Where(f => !f.IsDeleted);

        if (type.HasValue)
            query = query.Where(f => f.FacilityType == type.Value);

        if (isActive.HasValue)
            query = query.Where(f => f.IsActive == isActive.Value);

        return query.OrderBy(f => f.FacilityCode).Select(MapToResponse);
    }

    public async Task<TrainingFacilityResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var facility = await _unitOfWork.TrainingFacilityRepository.GetByIdAsync(id, cancellationToken);
        if (facility == null || facility.IsDeleted)
            throw new KeyNotFoundException($"Cơ sở đào tạo với ID {id} không tồn tại.");

        return MapToResponse(facility);
    }

    public async Task<IEnumerable<TrainingFacilityResponse>> GetAvailableFacilitiesAsync(
        DateTime startAt,
        DateTime endAt,
        FacilityType? type = null,
        int? excludeSessionId = null,
        CancellationToken cancellationToken = default)
    {
        if (startAt >= endAt)
            throw new ValidationException("Thời gian kết thúc phải sau thời gian bắt đầu.");

        var allFacilities = (await _unitOfWork.TrainingFacilityRepository.GetAllAsync(cancellationToken))
            .Where(f => !f.IsDeleted && f.IsActive)
            .ToList();

        if (type.HasValue)
            allFacilities = allFacilities.Where(f => f.FacilityType == type.Value).ToList();

        var sessions = (await _unitOfWork.SessionRepository.GetAllAsync(cancellationToken))
            .Where(s => !s.IsDeleted && s.FacilityId.HasValue)
            .ToList();

        var occupiedFacilityIds = new HashSet<int>();

        foreach (var s in sessions)
        {
            if (excludeSessionId.HasValue && s.SessionId == excludeSessionId.Value)
                continue;

            DateTime sStart = s.StartAt ?? s.SessionDate ?? DateTime.MinValue;
            DateTime sEnd = s.EndAt ?? (s.SessionDate.HasValue ? s.SessionDate.Value.AddHours(2) : sStart.AddHours(2));

            if (FacilityCompatibilityHelper.HasTimeOverlap(startAt, endAt, sStart, sEnd))
            {
                occupiedFacilityIds.Add(s.FacilityId!.Value);
            }
        }

        return allFacilities
            .Where(f => !occupiedFacilityIds.Contains(f.FacilityId))
            .OrderBy(f => f.FacilityCode)
            .Select(MapToResponse);
    }

    public async Task<TrainingFacilityResponse> CreateAsync(
        CreateTrainingFacilityRequest request,
        int createdByAccountId,
        CancellationToken cancellationToken = default)
    {
        var code = request.FacilityCode.Trim().ToUpperInvariant();
        var exists = (await _unitOfWork.TrainingFacilityRepository.GetAllAsync(cancellationToken))
            .Any(f => !f.IsDeleted && string.Equals(f.FacilityCode, code, StringComparison.OrdinalIgnoreCase));

        if (exists)
            throw new BusinessRuleViolationException($"Mã cơ sở đào tạo '{code}' đã tồn tại trong hệ thống.");

        var facility = new TrainingFacility
        {
            FacilityCode = code,
            FacilityName = request.FacilityName.Trim(),
            FacilityType = request.FacilityType,
            Capacity = request.Capacity,
            IsActive = request.IsActive,
            Description = request.Description?.Trim(),
            LocationDetail = request.LocationDetail?.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedByAccountId = createdByAccountId
        };

        await _unitOfWork.TrainingFacilityRepository.AddAsync(facility, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            AccountId = createdByAccountId,
            ActionType = AuditActionType.INSERT.ToString(),
            EntityName = nameof(TrainingFacility),
            RecordId = facility.FacilityId,
            NewValue = facility.FacilityCode,
            Description = $"Created Training Facility '{facility.FacilityName}' ({facility.FacilityCode})"
        }, cancellationToken);

        await _unitOfWork.SaveAsync(cancellationToken);

        return MapToResponse(facility);
    }

    public async Task<TrainingFacilityResponse> UpdateAsync(
        int id,
        UpdateTrainingFacilityRequest request,
        int updatedByAccountId,
        CancellationToken cancellationToken = default)
    {
        var facility = await _unitOfWork.TrainingFacilityRepository.GetByIdAsync(id, cancellationToken);
        if (facility == null || facility.IsDeleted)
            throw new KeyNotFoundException($"Cơ sở đào tạo với ID {id} không tồn tại.");

        var code = request.FacilityCode.Trim().ToUpperInvariant();
        var exists = (await _unitOfWork.TrainingFacilityRepository.GetAllAsync(cancellationToken))
            .Any(f => f.FacilityId != id && !f.IsDeleted && string.Equals(f.FacilityCode, code, StringComparison.OrdinalIgnoreCase));

        if (exists)
            throw new BusinessRuleViolationException($"Mã cơ sở đào tạo '{code}' đã tồn tại trong hệ thống.");

        facility.FacilityCode = code;
        facility.FacilityName = request.FacilityName.Trim();
        facility.FacilityType = request.FacilityType;
        facility.Capacity = request.Capacity;
        facility.IsActive = request.IsActive;
        facility.Description = request.Description?.Trim();
        facility.LocationDetail = request.LocationDetail?.Trim();
        facility.UpdatedAt = DateTime.UtcNow;
        facility.UpdatedByAccountId = updatedByAccountId;

        await _unitOfWork.TrainingFacilityRepository.UpdateAsync(facility, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            AccountId = updatedByAccountId,
            ActionType = AuditActionType.UPDATE.ToString(),
            EntityName = nameof(TrainingFacility),
            RecordId = facility.FacilityId,
            NewValue = facility.FacilityCode,
            Description = $"Updated Training Facility '{facility.FacilityName}' ({facility.FacilityCode})"
        }, cancellationToken);

        await _unitOfWork.SaveAsync(cancellationToken);

        return MapToResponse(facility);
    }

    public async Task DeleteAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default)
    {
        var facility = await _unitOfWork.TrainingFacilityRepository.GetByIdAsync(id, cancellationToken);
        if (facility == null || facility.IsDeleted)
            throw new KeyNotFoundException($"Cơ sở đào tạo với ID {id} không tồn tại.");

        facility.IsDeleted = true;
        facility.DeletedAt = DateTime.UtcNow;
        facility.DeletedByAccountId = deletedByAccountId;

        await _unitOfWork.TrainingFacilityRepository.UpdateAsync(facility, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            AccountId = deletedByAccountId,
            ActionType = AuditActionType.DELETE.ToString(),
            EntityName = nameof(TrainingFacility),
            RecordId = facility.FacilityId,
            Description = $"Deleted Training Facility '{facility.FacilityName}' ({facility.FacilityCode})"
        }, cancellationToken);

        await _unitOfWork.SaveAsync(cancellationToken);
    }

    private static TrainingFacilityResponse MapToResponse(TrainingFacility facility)
    {
        return new TrainingFacilityResponse
        {
            FacilityId = facility.FacilityId,
            FacilityCode = facility.FacilityCode,
            FacilityName = facility.FacilityName,
            FacilityType = facility.FacilityType,
            Capacity = facility.Capacity,
            IsActive = facility.IsActive,
            Description = facility.Description,
            LocationDetail = facility.LocationDetail
        };
    }
}
