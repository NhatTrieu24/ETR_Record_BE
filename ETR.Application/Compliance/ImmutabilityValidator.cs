using ETR.Application.Exceptions;
using ETR.Domain.Entities;

namespace ETR.Application.Compliance;

public enum EntityChangeAction
{
    Insert,
    Update,
    Delete
}

public sealed class EntityChangeSnapshot
{
    public required string EntityName { get; init; }
    public required EntityChangeAction Action { get; init; }
    public string? OriginalEtrStatus { get; init; }
    public bool? OriginalEtrIsLocked { get; init; }
    public bool IsBeingUnlocked { get; init; }
    public bool OriginalAssessmentIsPublished { get; init; }
    public bool IsScoreModified { get; init; }
    public bool IsBeingUnpublished { get; init; }
}

public static class ImmutabilityValidator
{
    private static readonly HashSet<string> EtrChildEntities =
    [
        nameof(SubjectResult),
        nameof(AttendanceRecord),
        nameof(AssessmentResult),
        nameof(EvidenceFile),
        nameof(PracticalChecklistResult),
        nameof(SubjectSignoff),
        nameof(RetakeHistory)
    ];

    public static void Validate(IReadOnlyList<EntityChangeSnapshot> changes)
    {
        foreach (var change in changes)
        {
            ValidateEtrRecord(change);
            ValidateEtrChildEntity(change);
            ValidatePublishedAssessmentScore(change);
        }
    }

    private static void ValidateEtrRecord(EntityChangeSnapshot change)
    {
        if (change.EntityName == nameof(ETRCourseRecord))
        {
            if (IsEtrImmutable(change.OriginalEtrStatus, change.OriginalEtrIsLocked) && !change.IsBeingUnlocked)
            {
                throw new ImmutabilityViolationException("Cannot modify ETRCourseRecord because it is Completed or Locked.");
            }
        }
    }

    private static void ValidateEtrChildEntity(EntityChangeSnapshot change)
    {
        if (EtrChildEntities.Contains(change.EntityName))
        {
            if (IsEtrImmutable(change.OriginalEtrStatus, change.OriginalEtrIsLocked))
            {
                throw new ImmutabilityViolationException($"Cannot modify {change.EntityName} because the related ETRCourseRecord is Completed or Locked.");
            }
        }
    }

    private static void ValidatePublishedAssessmentScore(EntityChangeSnapshot change)
    {
        // Allow score modification if the result is being unpublished (retake scenario)
        if (change.EntityName == nameof(AssessmentResult) && change.IsScoreModified && change.OriginalAssessmentIsPublished && !change.IsBeingUnpublished)
        {
            throw new ImmutabilityViolationException("Cannot modify score of a published AssessmentResult.");
        }
    }

    private static bool IsEtrImmutable(string? status, bool? isLocked)
    {
        return string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase)
            || isLocked == true;
    }
}
