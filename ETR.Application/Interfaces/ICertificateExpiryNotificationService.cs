namespace ETR.Application.Interfaces;

public interface ICertificateExpiryNotificationService
{
    /// <summary>Scans for certificates whose ExpiryDate is exactly 3, 7, or 30 days away and emails
    /// each affected student a reminder. Returns the number of reminder emails actually sent.</summary>
    Task<int> SendExpiryRemindersAsync(CancellationToken cancellationToken = default);
}
