using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs.Approval;

public class CreateApprovalRequest
{
    [Required]
    public int ETRCourseRecordId { get; set; }

    public int? CurrentApproverId { get; set; }
}
