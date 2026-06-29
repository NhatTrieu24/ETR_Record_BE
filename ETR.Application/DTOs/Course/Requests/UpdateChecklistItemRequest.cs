namespace ETR.Application.DTOs;

public record UpdateChecklistItemRequest(int ETRChecklistItemId, int TemplateId, string ItemName, string? Description, bool IsRequired, int DisplayOrder);
