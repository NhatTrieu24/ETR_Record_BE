using System.Text.Json;
using ETR.Application.Compliance;
using ETR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ETR.Infrastructure.Data;

public partial class AppDbContext
{
    private static readonly JsonSerializerOptions AuditJsonOptions = new()
    {
        WriteIndented = false
    };

    public override int SaveChanges()
    {
        return SaveChangesAsync().GetAwaiter().GetResult();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await EnforceImmutabilityAsync(cancellationToken);

        var pendingAudits = CapturePendingAuditEntries();
        var changeCount = await base.SaveChangesAsync(cancellationToken);

        if (pendingAudits.Count == 0)
        {
            return changeCount;
        }

        AuditLogs.AddRange(pendingAudits.Select(pending => pending.ToAuditLog()));
        changeCount += await base.SaveChangesAsync(cancellationToken);

        return changeCount;
    }

    private async Task EnforceImmutabilityAsync(CancellationToken cancellationToken)
    {
        var snapshots = await BuildImmutabilitySnapshotsAsync(cancellationToken);
        ImmutabilityValidator.Validate(snapshots);
    }

    private async Task<List<EntityChangeSnapshot>> BuildImmutabilitySnapshotsAsync(
        CancellationToken cancellationToken)
    {
        var snapshots = new List<EntityChangeSnapshot>();
        var entries = ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (entries.Count == 0)
        {
            return snapshots;
        }

        var etrContextByRecordId = await LoadEtrRecordContextAsync(entries, cancellationToken);

        foreach (var entry in entries)
        {
            var entityName = entry.Entity.GetType().Name;
            var action = entry.State == EntityState.Deleted
                ? EntityChangeAction.Delete
                : EntityChangeAction.Update;

            if (entityName == nameof(ETRRecord))
            {
                snapshots.Add(new EntityChangeSnapshot
                {
                    EntityName = entityName,
                    Action = action,
                    OriginalEtrStatus = entry.Property(nameof(ETRRecord.Status)).OriginalValue as string,
                    OriginalEtrIsLocked = entry.Property(nameof(ETRRecord.IsLocked)).OriginalValue as bool?
                });

                continue;
            }

            var etrRecordId = ResolveEtrRecordId(entry);
            etrContextByRecordId.TryGetValue(etrRecordId, out var etrContext);

            snapshots.Add(new EntityChangeSnapshot
            {
                EntityName = entityName,
                Action = action,
                OriginalEtrStatus = etrContext?.Status,
                OriginalEtrIsLocked = etrContext?.IsLocked,
                OriginalAssessmentIsPublished = entityName == nameof(AssessmentResult)
                    && entry.Property(nameof(AssessmentResult.IsPublished)).OriginalValue is true,
                IsScoreModified = entityName == nameof(AssessmentResult)
                    && entry.Property(nameof(AssessmentResult.Score)).IsModified
            });
        }

        return snapshots;
    }

    private async Task<Dictionary<int, EtrRecordContext>> LoadEtrRecordContextAsync(
        IEnumerable<EntityEntry> entries,
        CancellationToken cancellationToken)
    {
        var etrRecordIds = entries
            .Select(ResolveEtrRecordId)
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (etrRecordIds.Count == 0)
        {
            return [];
        }

        return await ETRRecords
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(e => etrRecordIds.Contains(e.ETRRecordId))
            .Select(e => new EtrRecordContext(e.ETRRecordId, e.Status, e.IsLocked))
            .ToDictionaryAsync(e => e.ETRRecordId, cancellationToken);
    }

    private static int ResolveEtrRecordId(EntityEntry entry)
    {
        return entry.Entity switch
        {
            ETRRecord record => record.ETRRecordId,
            ETRChecklistProgress progress => GetOriginalOrCurrent(entry, progress.ETRRecordId, nameof(ETRChecklistProgress.ETRRecordId)),
            AttendanceRecord attendance => GetOriginalOrCurrent(entry, attendance.ETRRecordId, nameof(AttendanceRecord.ETRRecordId)),
            AssessmentResult assessment => GetOriginalOrCurrent(entry, assessment.ETRRecordId, nameof(AssessmentResult.ETRRecordId)),
            EvidenceFile evidence => GetOriginalOrCurrent(entry, evidence.ETRRecordId, nameof(EvidenceFile.ETRRecordId)),
            _ => 0
        };
    }

    private static int GetOriginalOrCurrent(EntityEntry entry, int currentValue, string propertyName)
    {
        if (entry.State == EntityState.Modified
            && entry.Properties.Any(p => p.Metadata.Name == propertyName)
            && entry.Property(propertyName).OriginalValue is int originalValue)
        {
            return originalValue;
        }

        return currentValue;
    }

    private List<PendingAuditEntry> CapturePendingAuditEntries()
    {
        return ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified)
            .Select(entry => new PendingAuditEntry(
                entry,
                entry.State == EntityState.Added ? "INSERT" : "UPDATE",
                entry.State == EntityState.Added ? null : SerializePropertyValues(entry.OriginalValues),
                SerializePropertyValues(entry.CurrentValues),
                ResolveAuditUserId(entry),
                ResolveAuditEtrRecordId(entry.Entity),
                entry.State == EntityState.Modified
                    && entry.Property(nameof(BaseEntity.IsDeleted)).IsModified
                    && entry.Entity.IsDeleted
                    ? "Soft delete"
                    : null))
            .ToList();
    }

    private static int? ResolveAuditUserId(EntityEntry<BaseEntity> entry)
    {
        if (entry.State == EntityState.Modified && entry.Entity.UpdatedBy.HasValue)
        {
            return entry.Entity.UpdatedBy;
        }

        return entry.Entity.CreatedBy;
    }

    private static int? ResolveAuditEtrRecordId(BaseEntity entity)
    {
        return entity switch
        {
            ETRRecord record => record.ETRRecordId,
            ETRChecklistProgress progress => progress.ETRRecordId,
            AttendanceRecord attendance => attendance.ETRRecordId,
            AssessmentResult assessment => assessment.ETRRecordId,
            EvidenceFile evidence => evidence.ETRRecordId,
            _ => null
        };
    }

    private static int GetPrimaryKeyValue(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey()
            ?? throw new InvalidOperationException($"Entity {entry.Metadata.Name} has no primary key.");

        var keyProperty = key.Properties[0];
        var keyValue = entry.Property(keyProperty.Name).CurrentValue;

        return keyValue switch
        {
            int intValue => intValue,
            long longValue when longValue <= int.MaxValue => (int)longValue,
            _ => throw new InvalidOperationException(
                $"Audit logging supports integer primary keys only. Entity: {entry.Metadata.Name}")
        };
    }

    private static string? SerializePropertyValues(PropertyValues? values)
    {
        if (values is null)
        {
            return null;
        }

        var payload = values.Properties.ToDictionary(
            property => property.Name,
            property => values[property.Name]);

        return JsonSerializer.Serialize(payload, AuditJsonOptions);
    }

    private sealed record EtrRecordContext(int ETRRecordId, string Status, bool IsLocked);

    private sealed class PendingAuditEntry
    {
        private readonly EntityEntry _entry;
        private readonly string _actionType;
        private readonly string? _oldValue;
        private readonly string? _newValue;
        private readonly int? _userId;
        private readonly int? _etrRecordId;
        private readonly string? _description;

        public PendingAuditEntry(
            EntityEntry entry,
            string actionType,
            string? oldValue,
            string? newValue,
            int? userId,
            int? etrRecordId,
            string? description)
        {
            _entry = entry;
            _actionType = actionType;
            _oldValue = oldValue;
            _newValue = newValue;
            _userId = userId;
            _etrRecordId = etrRecordId;
            _description = description;
        }

        public AuditLog ToAuditLog()
        {
            return new AuditLog
            {
                UserId = _userId,
                ETRRecordId = _etrRecordId,
                ActionType = _actionType,
                EntityName = _entry.Entity.GetType().Name,
                RecordId = GetPrimaryKeyValue(_entry),
                OldValue = _oldValue,
                NewValue = _newValue,
                Description = _description
            };
        }
    }
}
