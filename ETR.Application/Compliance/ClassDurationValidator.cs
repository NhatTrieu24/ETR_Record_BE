namespace ETR.Application.Compliance;

/// <summary>
/// Quản lý thời gian đào tạo lớp học (Class Duration) theo tiêu chuẩn ICAO/CAAV.
/// MaxDailyHours = 8h đối với môn Lý thuyết (Ground), 4h đối với môn Thực hành/Buồng lái mô phỏng (SIM/Practical).
/// Thời gian đệm 15% (Buffer Days) cho Retake và SIM maintenance.
/// </summary>
public static class ClassDurationValidator
{
    public const double GroundMaxDailyHours = 8.0;
    public const double SimMaxDailyHours = 4.0;
    public const double BufferPercentage = 0.15;

    public static (int MinTrainingDays, int MinBufferDays, int TotalMinDays, DateTime MinEndDate) CalculateMinDuration(
        DateTime startDate,
        IEnumerable<(int RequiredHours, string? SubjectType)> subjects)
    {
        double totalDays = 0;
        foreach (var (hours, type) in subjects)
        {
            if (hours <= 0) continue;
            bool isSim = !string.IsNullOrEmpty(type) &&
                         (type.Contains("Practical", StringComparison.OrdinalIgnoreCase) ||
                          type.Contains("SIM", StringComparison.OrdinalIgnoreCase) ||
                          type.Contains("Simulator", StringComparison.OrdinalIgnoreCase));
            double maxDaily = isSim ? SimMaxDailyHours : GroundMaxDailyHours;
            totalDays += hours / maxDaily;
        }

        int minTrainingDays = (int)Math.Ceiling(totalDays);
        if (minTrainingDays <= 0) minTrainingDays = 1;

        int minBufferDays = (int)Math.Ceiling(minTrainingDays * BufferPercentage);
        int totalMinDays = minTrainingDays + minBufferDays;
        DateTime minEndDate = startDate.Date.AddDays(totalMinDays);

        return (minTrainingDays, minBufferDays, totalMinDays, minEndDate);
    }
}
