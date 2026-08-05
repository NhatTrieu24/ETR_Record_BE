using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs;

public class AddCourseSubjectRequest
{
    [Required]
    public int SubjectId { get; set; }
    
    [Required]
    public int SequenceNo { get; set; }
    
    public int RequiredHours { get; set; } = 0;
    
    public bool IsMandatory { get; set; } = true;
    
    [Range(0, 100)]
    public decimal PassingScore { get; set; } = 5.0m;
}
