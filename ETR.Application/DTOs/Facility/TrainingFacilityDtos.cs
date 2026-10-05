using System.ComponentModel.DataAnnotations;
using ETR.Domain.Enums;

namespace ETR.Application.DTOs.Facility;

public class CreateTrainingFacilityRequest
{
    [Required]
    [MaxLength(50)]
    public string FacilityCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string FacilityName { get; set; } = string.Empty;

    [Required]
    public FacilityType FacilityType { get; set; } = FacilityType.Classroom;

    [Range(1, 1000)]
    public int Capacity { get; set; } = 30;

    public bool IsActive { get; set; } = true;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(250)]
    public string? LocationDetail { get; set; }
}

public class UpdateTrainingFacilityRequest
{
    [Required]
    [MaxLength(50)]
    public string FacilityCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string FacilityName { get; set; } = string.Empty;

    [Required]
    public FacilityType FacilityType { get; set; } = FacilityType.Classroom;

    [Range(1, 1000)]
    public int Capacity { get; set; } = 30;

    public bool IsActive { get; set; } = true;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(250)]
    public string? LocationDetail { get; set; }
}

public class TrainingFacilityResponse
{
    public int FacilityId { get; set; }
    public string FacilityCode { get; set; } = string.Empty;
    public string FacilityName { get; set; } = string.Empty;
    public FacilityType FacilityType { get; set; } = FacilityType.Classroom;
    public string FacilityTypeName => FacilityType.ToString();
    public int Capacity { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public string? LocationDetail { get; set; }
}
