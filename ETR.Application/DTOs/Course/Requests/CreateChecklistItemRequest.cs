namespace ETR.Application.DTOs;

public record CreateChecklistItemRequest(int TemplateId, string ItemName, string? Description, bool IsRequired, int DisplayOrder);
