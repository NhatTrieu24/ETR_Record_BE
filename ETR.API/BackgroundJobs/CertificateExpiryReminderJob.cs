using ETR.Application.Interfaces;
using ETR.Application.Services;

namespace ETR.API.BackgroundJobs;

/// <summary>Runs once a day at <see cref="RunAtUtc"/> and asks <see cref="ICertificateExpiryNotificationService"/>
/// to email every student whose certificate is exactly 3, 7, or 30 days from expiry. Scheduling to a
/// fixed time-of-day (via CertificateExpiryScheduleCalculator) means a redeploy mid-day never causes a
/// same-day double run — see that class for the pure delay math (unit-tested in ETR.Application.Tests).</summary>
public class CertificateExpiryReminderJob : BackgroundService
{
    private static readonly TimeSpan RunAtUtc = TimeSpan.FromHours(6);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CertificateExpiryReminderJob> _logger;

    public CertificateExpiryReminderJob(IServiceScopeFactory scopeFactory, ILogger<CertificateExpiryReminderJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = CertificateExpiryScheduleCalculator.GetDelayUntilNextRun(DateTime.UtcNow, RunAtUtc);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var notifier = scope.ServiceProvider.GetRequiredService<ICertificateExpiryNotificationService>();
                var sentCount = await notifier.SendExpiryRemindersAsync(stoppingToken);
                _logger.LogInformation("Certificate expiry reminder job sent {Count} email(s).", sentCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Certificate expiry reminder job failed.");
            }
        }
    }
}
