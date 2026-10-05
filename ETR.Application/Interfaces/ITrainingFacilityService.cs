using ETR.Application.DTOs.Facility;
using ETR.Domain.Enums;

namespace ETR.Application.Interfaces;

public interface ITrainingFacilityService
{
    Task<IEnumerable<TrainingFacilityResponse>> GetAllAsync(FacilityType? type = null, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<TrainingFacilityResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<TrainingFacilityResponse>> GetAvailableFacilitiesAsync(DateTime startAt, DateTime endAt, FacilityType? type = null, int? excludeSessionId = null, CancellationToken cancellationToken = default);
    Task<TrainingFacilityResponse> CreateAsync(CreateTrainingFacilityRequest request, int createdByAccountId, CancellationToken cancellationToken = default);
    Task<TrainingFacilityResponse> UpdateAsync(int id, UpdateTrainingFacilityRequest request, int updatedByAccountId, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default);
}
