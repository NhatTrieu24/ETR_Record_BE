using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs.PracticalChecklist;

public class UpdatePracticalChecklistRequest
{
    [Required]
    public string ItemName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsRequired { get; set; }
    
    public int DisplayOrder { get; set; }
}
