using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs.Amendment.Requests;

public class CreateAmendmentRequestRequest
{
    [Required]
    public string Reason { get; set; } = string.Empty;
}
