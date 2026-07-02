namespace ETR.Application.DTOs;

public record BulkAttendanceRequest(List<BulkAttendanceItemRequest> Records, int RecordedByUserId);
