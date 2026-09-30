using ETR.Domain.Enums;
using System.Text.Json.Serialization;

namespace ETR.Application.DTOs;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReadinessStatus
{
    Met,
    NotMet,
    NoData,
    ReviewRequired
}

public record ReadinessItemDto(
    string ConditionCode,
    string ConditionName,
    ReadinessStatus Status,
    decimal? CurrentValue,
    decimal? ThresholdValue,
    string? Unit,
    bool IsMandatory,
    string Explanation,
    string? Category
);

public record ReadinessWarningDto(
    string WarningCode,
    string Message,
    string Severity, // "Warning", "Info", "ActionRequired"
    string? Category
);

public record EtrReadinessResponse(
    int ETRCourseRecordId,
    int EnrollmentId,
    int AccountId,
    string StudentName,
    int CourseId,
    string CourseName,
    int CourseVersionNo,
    int ClassId,
    string ClassName,
    ReadinessStatus OverallStatus,
    decimal TotalFlightHours,
    decimal TotalSimulatorHours,
    DateTime EvaluatedAt,
    IReadOnlyList<ReadinessItemDto> Conditions,
    IReadOnlyList<ReadinessWarningDto> Warnings,
    string Disclaimer = "Đánh giá mức độ sẵn sàng đào tạo dựa trên dữ liệu ETR và phiên bản giáo trình áp dụng. Đây là tín hiệu tham chiếu, không thay thế quyết định phê duyệt chuyên môn hoặc cấp phép bay."
);
