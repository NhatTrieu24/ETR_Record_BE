using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs;

public record GoogleLoginRequest([Required] string IdToken);
