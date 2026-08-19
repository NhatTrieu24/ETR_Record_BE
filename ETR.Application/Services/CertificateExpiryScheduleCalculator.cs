namespace ETR.Application.Services;

/// <summary>Pure delay-until-next-run math for <c>CertificateExpiryReminderJob</c>, split out so it's
/// unit-testable without a hosted-service test harness. Scheduling to a fixed time-of-day (rather than
/// "run every 24h from process start") means a redeploy mid-day does not cause a same-day double run.</summary>
public static class CertificateExpiryScheduleCalculator
{
    public static TimeSpan GetDelayUntilNextRun(DateTime nowUtc, TimeSpan runAtUtc)
    {
        var todayRun = nowUtc.Date.Add(runAtUtc);
        var nextRun = nowUtc < todayRun ? todayRun : todayRun.AddDays(1);
        return nextRun - nowUtc;
    }
}
