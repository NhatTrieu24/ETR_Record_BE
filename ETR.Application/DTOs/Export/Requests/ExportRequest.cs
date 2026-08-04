namespace ETR.Application.DTOs;

public record ExportRequest(int UserId, int? ETRCourseRecordId = null, int? ClassId = null);
