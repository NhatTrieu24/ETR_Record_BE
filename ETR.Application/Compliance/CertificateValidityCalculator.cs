using ETR.Application.Interfaces;

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
}
