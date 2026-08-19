using ETR.Application.Services;

namespace ETR.Application.Tests.Services;

public class CertificateExpiryScheduleCalculatorTests
{
    [Fact]
    public void GetDelayUntilNextRun_ReturnsDelayLaterToday_WhenRunTimeHasNotPassedYetToday()
    {
        var now = new DateTime(2026, 8, 20, 3, 0, 0, DateTimeKind.Utc);
        var runAt = TimeSpan.FromHours(6);

        var delay = CertificateExpiryScheduleCalculator.GetDelayUntilNextRun(now, runAt);

        Assert.Equal(TimeSpan.FromHours(3), delay);
    }

    [Fact]
    public void GetDelayUntilNextRun_ReturnsDelayTomorrow_WhenRunTimeHasAlreadyPassedToday()
    {
        var now = new DateTime(2026, 8, 20, 9, 0, 0, DateTimeKind.Utc);
        var runAt = TimeSpan.FromHours(6);

        var delay = CertificateExpiryScheduleCalculator.GetDelayUntilNextRun(now, runAt);

        Assert.Equal(TimeSpan.FromHours(21), delay);
    }

    [Fact]
    public void GetDelayUntilNextRun_ReturnsZero_WhenNowIsExactlyRunTime()
    {
        var now = new DateTime(2026, 8, 20, 6, 0, 0, DateTimeKind.Utc);
        var runAt = TimeSpan.FromHours(6);

        var delay = CertificateExpiryScheduleCalculator.GetDelayUntilNextRun(now, runAt);

        Assert.Equal(TimeSpan.FromDays(1), delay);
    }
}
