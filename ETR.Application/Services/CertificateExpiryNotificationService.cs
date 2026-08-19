using ETR.Application.Compliance;
using ETR.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ETR.Application.Services;

public class CertificateExpiryNotificationService : ICertificateExpiryNotificationService
{
    private static readonly int[] ThresholdDays = [3, 7, 30];

    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ILogger<CertificateExpiryNotificationService> _logger;
    private readonly Func<DateTime> _nowProvider;

    public CertificateExpiryNotificationService(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ILogger<CertificateExpiryNotificationService> logger,
        Func<DateTime>? nowProvider = null)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _logger = logger;
        _nowProvider = nowProvider ?? (() => DateTime.UtcNow);
    }

    public async Task<int> SendExpiryRemindersAsync(CancellationToken cancellationToken = default)
    {
        var candidates = await CertificateValidityCalculator.GetCertificatesNearingExpiryAsync(
            _unitOfWork, ThresholdDays, _nowProvider(), cancellationToken);

        if (candidates.Count == 0)
        {
            return 0;
        }

        var accounts = await _unitOfWork.AccountRepository.GetAllAsync(cancellationToken);
        var profiles = await _unitOfWork.UserProfileRepository.GetAllAsync(cancellationToken);
        var courses = await _unitOfWork.CourseRepository.GetAllAsync(cancellationToken);

        var sentCount = 0;

        foreach (var candidate in candidates)
        {
            var profile = profiles.FirstOrDefault(p => p.AccountId == candidate.AccountId);
            var email = profile?.Email;
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning(
                    "Bỏ qua nhắc hết hạn chứng chỉ cho AccountId {AccountId}: không có email.",
                    candidate.AccountId);
                continue;
            }

            var account = accounts.FirstOrDefault(a => a.AccountId == candidate.AccountId);
            var course = courses.FirstOrDefault(c => c.CourseId == candidate.CourseId);
            var fullName = profile!.FullName is { Length: > 0 } ? profile.FullName : (account?.Username ?? "Học viên");
            var courseName = course?.CourseName ?? $"Khóa học #{candidate.CourseId}";

            var tokens = new Dictionary<string, string>
            {
                ["FullName"] = fullName,
                ["CourseName"] = courseName,
                ["ExpiryDate"] = candidate.ExpiryDate.ToString("dd/MM/yyyy"),
                ["DaysRemaining"] = candidate.DaysUntilExpiry.ToString()
            };

            try
            {
                await _emailService.SendTemplatedEmailAsync(
                    email,
                    fullName,
                    "CertificateExpiryReminder.html",
                    $"Chứng chỉ '{courseName}' của bạn sẽ hết hạn trong {candidate.DaysUntilExpiry} ngày",
                    tokens,
                    cancellationToken);
                sentCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Gửi email nhắc hết hạn chứng chỉ thất bại cho AccountId {AccountId}, ETRCourseRecordId {ETRCourseRecordId}.",
                    candidate.AccountId, candidate.ETRCourseRecordId);
            }
        }

        return sentCount;
    }
}
