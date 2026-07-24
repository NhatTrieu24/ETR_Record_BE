using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs;

// Comment bắt buộc khi Return (EtrService.ReturnEtrAsync cũng validate lại — xem P2 mục 30);
// [Required] ở đây giúp trả 400 sớm hơn, trước khi chạm tới service.
public record ReturnEtrRequest(
    [Required(AllowEmptyStrings = false), MaxLength(1000)] string? Comment
);
