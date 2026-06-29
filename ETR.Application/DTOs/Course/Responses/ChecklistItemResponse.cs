namespace ETR.Application.DTOs;

public record ChecklistItemResponse(int ETRChecklistItemId, int TemplateId, string ItemName, string? Description, bool IsRequired, int DisplayOrder);
