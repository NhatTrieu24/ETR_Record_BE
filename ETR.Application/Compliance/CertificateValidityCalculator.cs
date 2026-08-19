using ETR.Application.Interfaces;
using ETR.Domain.Enums;

namespace ETR.Application.Compliance;

/// <summary>Shared certificate-expiry check used both by the on-demand Grounded-status refresh
/// (EtrService.RefreshGroundedStatusAsync) and by re-enrollment (EnrollmentService.CreateEnrollmentAsync)
/// so the two call sites can never drift into disagreeing about what "expired" means.</summary>
public static class CertificateValidityCalculator
{
    /// <summary>
    /// True if, for at least one Course the learner has ever enrolled in, the most recently issued
    /// ETRCourseRecord for that course (ordered by IssuedDate, falling back to CreatedAt for records
    /// not yet completed) has an ExpiryDate already in the past.
    ///
    /// A fresh re-enrollment becomes the "most recent" record for its course as soon as it is saved
    /// (higher CreatedAt, ExpiryDate still null) — so this naturally flips back to false for that
    /// course the moment re-enrollment succeeds, with no separate bookkeeping needed.
    /// </summary>
    public static async Task<bool> HasAnyExpiredCompletedEtrAsync(IUnitOfWork unitOfWork, int accountId, CancellationToken cancellationToken)
    {
        var enrollments = (await unitOfWork.CourseEnrollmentRepository.GetAllAsync(cancellationToken))
            .Where(e => e.AccountId == accountId)
            .ToList();
        if (enrollments.Count == 0) return false;

        var classes = await unitOfWork.ClassRepository.GetAllAsync(cancellationToken);
        var etrs = await unitOfWork.ETRCourseRecordRepository.GetAllAsync(cancellationToken);

        var groupedByCourse = enrollments
            .Join(classes, e => e.ClassId, c => c.ClassId, (e, c) => new { e.EnrollmentId, c.CourseId })
            .Join(etrs, ec => ec.EnrollmentId, etr => etr.EnrollmentId, (ec, etr) => new { ec.CourseId, Etr = etr })
            .GroupBy(x => x.CourseId);

        foreach (var group in groupedByCourse)
        {
            var latestEtr = group.OrderByDescending(x => x.Etr.IssuedDate ?? x.Etr.CreatedAt).First().Etr;
            if (latestEtr.ExpiryDate.HasValue && latestEtr.ExpiryDate.Value < DateTime.UtcNow)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// For every (Account, Course) pair, looks at only the most recently issued Completed
    /// ETRCourseRecord (same "latest wins" rule as <see cref="HasAnyExpiredCompletedEtrAsync"/>) and
    /// returns it if its ExpiryDate is exactly N days away, where N is one of <paramref name="thresholdDays"/>.
    /// Used by the certificate-expiry reminder job — each record fires at most once per threshold per
    /// day the job runs, since "days until expiry" only equals a given threshold on one calendar day.
    /// </summary>
    public static async Task<IReadOnlyList<CertificateExpiryCandidate>> GetCertificatesNearingExpiryAsync(
        IUnitOfWork unitOfWork,
        IReadOnlyCollection<int> thresholdDays,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var enrollments = await unitOfWork.CourseEnrollmentRepository.GetAllAsync(cancellationToken);
        var classes = await unitOfWork.ClassRepository.GetAllAsync(cancellationToken);
        var etrs = await unitOfWork.ETRCourseRecordRepository.GetAllAsync(cancellationToken);

        var joined = enrollments
            .Join(classes, e => e.ClassId, c => c.ClassId, (e, c) => new { e.AccountId, e.EnrollmentId, c.CourseId })
            .Join(etrs, ec => ec.EnrollmentId, etr => etr.EnrollmentId, (ec, etr) => new { ec.AccountId, ec.CourseId, Etr = etr });

        var results = new List<CertificateExpiryCandidate>();

        foreach (var group in joined.GroupBy(x => new { x.AccountId, x.CourseId }))
        {
            var latest = group.OrderByDescending(x => x.Etr.IssuedDate ?? x.Etr.CreatedAt).First();
            var etr = latest.Etr;

            if (etr.Status != EtrStatus.Completed || !etr.ExpiryDate.HasValue)
            {
                continue;
            }

            var daysUntilExpiry = (etr.ExpiryDate.Value.Date - nowUtc.Date).Days;
            if (thresholdDays.Contains(daysUntilExpiry))
            {
                results.Add(new CertificateExpiryCandidate(
                    latest.AccountId, latest.CourseId, etr.ETRCourseRecordId, etr.ExpiryDate.Value, daysUntilExpiry));
            }
        }

        return results;
    }
}
