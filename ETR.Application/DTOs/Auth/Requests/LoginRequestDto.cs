using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs;

public record LoginRequestDto(
    [Required] string Username,
    [Required] string Password);
