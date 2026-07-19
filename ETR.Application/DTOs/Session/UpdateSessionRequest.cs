using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs.Session;

public class UpdateSessionRequest
{
    [Required]
    [MaxLength(200)]
    public string SessionTitle { get; set; } = string.Empty;

    [Required]
    public DateTime SessionDate { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }
}
