namespace ETR.Application.Compliance;

public sealed record CertificateExpiryCandidate(
    int AccountId,
    int CourseId,
    int ETRCourseRecordId,
    DateTime ExpiryDate,
    int DaysUntilExpiry);
