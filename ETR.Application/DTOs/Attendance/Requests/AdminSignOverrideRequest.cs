using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs;

public record AdminSignOverrideRequest(
    [Required(ErrorMessage = "Lý do ký thay là bắt buộc.")]
    [MinLength(10, ErrorMessage = "Lý do ký thay phải có ít nhất 10 ký tự.")]
    string Reason,
    [MaxLength(1000)] string? Comments = null
);
