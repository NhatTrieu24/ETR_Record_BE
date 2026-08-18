namespace ETR.Application.Compliance;

public static class BusinessRuleEngine
{
    public const decimal MinimumAttendanceThreshold = 80.0m;

    /// <summary>Total attempts allowed per Assessment (initial attempt + retakes).</summary>
    public const int MaxAssessmentAttempts = 3;

    /// <summary>Thời gian ân hạn tối đa cho phép Giảng viên điểm danh bù sau ngày học thực tế (48 giờ).</summary>
    public const int AttendanceGracePeriodHours = 48;
}
