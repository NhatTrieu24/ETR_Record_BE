using ETR.Domain.Enums;

namespace ETR.Domain.Entities;

/// <summary>
/// Thực thể Cơ sở vật chất / Địa điểm đào tạo (Training Facility / Location).
/// </summary>
public class TrainingFacility : BaseEntity
{
    public int FacilityId { get; set; }
    public string FacilityCode { get; set; } = string.Empty;
    public string FacilityName { get; set; } = string.Empty;
    public FacilityType FacilityType { get; set; } = FacilityType.Classroom;
    public int Capacity { get; set; } = 30;
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public string? LocationDetail { get; set; }
}
