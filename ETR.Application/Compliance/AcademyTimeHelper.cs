namespace ETR.Application.Compliance;

/// <summary>
/// Chuẩn hóa ngày nghiệp vụ và múi giờ của Học viện (mặc định UTC+7 / SE Asia Standard Time).
/// Đảm bảo tính toán ngày (Date) nhất quán giữa API request, Excel import và thời gian thực tế.
/// </summary>
public static class AcademyTimeHelper
{
    public static readonly TimeZoneInfo AcademyTimeZone = ResolveAcademyTimeZone();

    private static TimeZoneInfo ResolveAcademyTimeZone()
    {
        if (TimeZoneInfo.TryFindSystemTimeZoneById("SE Asia Standard Time", out var tz)) return tz;
        if (TimeZoneInfo.TryFindSystemTimeZoneById("Asia/Ho_Chi_Minh", out tz)) return tz;
        if (TimeZoneInfo.TryFindSystemTimeZoneById("Asia/Bangkok", out tz)) return tz;
        return TimeZoneInfo.CreateCustomTimeZone("AcademyTimeZone", TimeSpan.FromHours(7), "Academy Time (UTC+7)", "Academy Time (UTC+7)");
    }

    /// <summary>
    /// Lấy ngày hôm nay theo múi giờ nghiệp vụ của học viện (00:00:00).
    /// </summary>
    public static DateTime GetToday()
    {
        var academyNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, AcademyTimeZone);
        return academyNow.Date;
    }

    /// <summary>
    /// Chuyển đổi một mốc thời gian (DateTime) sang ngày theo múi giờ học viện.
    /// </summary>
    public static DateTime ToAcademyDate(DateTime dateTime)
    {
        if (dateTime.Kind == DateTimeKind.Utc)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(dateTime, AcademyTimeZone).Date;
        }

        if (dateTime.Kind == DateTimeKind.Local)
        {
            return TimeZoneInfo.ConvertTime(dateTime, AcademyTimeZone).Date;
        }

        // DateTimeKind.Unspecified (ví dụ parse từ chuỗi ngày dd/MM/yyyy hoặc ngày thuần túy)
        return dateTime.Date;
    }

    /// <summary>
    /// Kiểm tra xem ngày của lớp học đã trôi qua trong quá khứ so với ngày hôm nay của học viện hay chưa.
    /// </summary>
    public static bool IsInPast(DateTime startDate)
    {
        return ToAcademyDate(startDate) < GetToday();
    }
}
