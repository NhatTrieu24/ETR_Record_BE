using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs;

public class UpdateCourseSubjectRequest
{
    [Required]
    public int SequenceNo { get; set; }
    
    public int RequiredHours { get; set; } = 0;
    
    [Required, Range(1, int.MaxValue)]
    public int RequiredSessions { get; set; }
    
    public bool IsMandatory { get; set; } = true;
    
    [Range(0, 100)]
    public decimal PassingScore { get; set; } = 5.0m;
}
