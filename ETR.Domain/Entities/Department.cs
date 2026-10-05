using System.ComponentModel.DataAnnotations;

namespace ETR.Domain.Entities;

public class Department : BaseEntity
{
    public int DepartmentId { get; set; }

    [Required, MaxLength(20)]
    public string DepartmentCode { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string DepartmentName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether this department represents a student training audience/specialization
    /// (e.g. Flight Crew, Cabin Crew, Engineering & Maintenance, Ground Operations)
    /// vs internal administrative staff (e.g. Administration, Training/Academic).
    /// Course enrollment restrictions only apply to departments where IsTrainingAudience is true.
    /// </summary>
    public bool IsTrainingAudience { get; set; } = true;
}
