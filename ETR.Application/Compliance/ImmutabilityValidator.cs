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
    public bool OriginalAssessmentIsPublished { get; init; }
    public bool IsScoreModified { get; init; }
}

public static class ImmutabilityValidator
{
    private static readonly HashSet<string> EtrChildEntities =
    [
        nameof(ETRChecklistProgress),
        nameof(AttendanceRecord),
        nameof(AssessmentResult),
        nameof(EvidenceFile)
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
        if (change.EntityName != nameof(ETRRecord))
        {
            return;
        }

        if (change.Action is not (EntityChangeAction.Update or EntityChangeAction.Delete))
        {
            return;
        }

        if (IsEtrImmutable(change.OriginalEtrStatus, change.OriginalEtrIsLocked))
        {
            throw new ImmutabilityViolationException(
                "ETR records that are Completed or locked cannot be modified or deleted.");
        }
    }

    private static void ValidateEtrChildEntity(EntityChangeSnapshot change)
    {
        if (!EtrChildEntities.Contains(change.EntityName))
        {
            return;
        }

        if (change.Action is not (EntityChangeAction.Update or EntityChangeAction.Delete))
        {
            return;
        }

        if (IsEtrImmutable(change.OriginalEtrStatus, change.OriginalEtrIsLocked))
        {
            throw new ImmutabilityViolationException(
                $"Cannot modify or delete {change.EntityName} because the linked ETR record is Completed or locked.");
        }
    }

    private static void ValidatePublishedAssessmentScore(EntityChangeSnapshot change)
    {
        if (change.EntityName != nameof(AssessmentResult))
        {
            return;
        }

        if (change.Action != EntityChangeAction.Update)
        {
            return;
        }

        if (change.OriginalAssessmentIsPublished && change.IsScoreModified)
        {
            throw new ImmutabilityViolationException(
                "Published assessment results cannot have their score modified.");
        }
    }

    private static bool IsEtrImmutable(string? status, bool? isLocked)
    {
        return string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase)
            || isLocked == true;
    }
}
