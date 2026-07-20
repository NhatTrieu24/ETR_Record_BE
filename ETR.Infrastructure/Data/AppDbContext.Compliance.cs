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

        var enrollmentIds = new HashSet<int>();
        foreach (var entry in entries)
        {
            var enrollmentId = await ResolveEnrollmentIdAsync(entry, cancellationToken);
            if (enrollmentId > 0) enrollmentIds.Add(enrollmentId);
        }

        var etrContextByEnrollmentId = new Dictionary<int, EtrRecordContext>();
        if (enrollmentIds.Count > 0)
        {
            etrContextByEnrollmentId = await ETRCourseRecords
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(e => enrollmentIds.Contains(e.EnrollmentId))
                .Select(e => new { e.EnrollmentId, Context = new EtrRecordContext(e.ETRCourseRecordId, e.Status, e.IsLocked) })
                .ToDictionaryAsync(e => e.EnrollmentId, e => e.Context, cancellationToken);
        }

        foreach (var entry in entries)
        {
            var entityName = entry.Entity.GetType().Name;
            var action = entry.State == EntityState.Deleted
                ? EntityChangeAction.Delete
                : EntityChangeAction.Update;

            if (entityName == nameof(ETRCourseRecord))
            {
                snapshots.Add(new EntityChangeSnapshot
                {
                    EntityName = entityName,
                    Action = action,
                    OriginalEtrStatus = entry.Property(nameof(ETRCourseRecord.Status)).OriginalValue as string,
                    OriginalEtrIsLocked = entry.Property(nameof(ETRCourseRecord.IsLocked)).OriginalValue as bool?
                });
                continue;
            }

            var enrollmentId = await ResolveEnrollmentIdAsync(entry, cancellationToken);
            etrContextByEnrollmentId.TryGetValue(enrollmentId, out var etrContext);

            snapshots.Add(new EntityChangeSnapshot
            {
                EntityName = entityName,
                Action = action,
                OriginalEtrStatus = etrContext?.Status,
                OriginalEtrIsLocked = etrContext?.IsLocked,
                OriginalAssessmentIsPublished = entityName == nameof(AssessmentResult)
                    && entry.Property(nameof(AssessmentResult.IsPublished)).OriginalValue is true,
                IsScoreModified = entityName == nameof(AssessmentResult)
                    && entry.Property(nameof(AssessmentResult.Score)).IsModified,
                IsBeingUnpublished = entityName == nameof(AssessmentResult)
                    && entry.Property(nameof(AssessmentResult.IsPublished)).OriginalValue is true
                    && entry.Property(nameof(AssessmentResult.IsPublished)).CurrentValue is false
            });
        }

        return snapshots;
    }

    private async Task<int> ResolveEnrollmentIdAsync(EntityEntry entry, CancellationToken cancellationToken)
    {
        if (entry.Entity is ETRCourseRecord record) return record.EnrollmentId;
        if (entry.Entity is CourseEnrollment enrollment) return enrollment.EnrollmentId;
        
        int etrId = 0;
        if (entry.Entity is SubjectResult sr) etrId = GetOriginalOrCurrent(entry, sr.EtrId, nameof(SubjectResult.EtrId));
        
        if (etrId > 0)
        {
            var etrEntry = ChangeTracker.Entries<ETRCourseRecord>().FirstOrDefault(e => e.Entity.ETRCourseRecordId == etrId);
            if (etrEntry != null) return etrEntry.Entity.EnrollmentId;

            var etrResult = await ETRCourseRecords.AsNoTracking().FirstOrDefaultAsync(e => e.ETRCourseRecordId == etrId, cancellationToken);
            if (etrResult != null) return etrResult.EnrollmentId;
        }

        if (entry.Entity is AttendanceRecord ar)
        {
            var classStudentId = GetOriginalOrCurrent(entry, ar.ClassStudentId, nameof(AttendanceRecord.ClassStudentId));
            var csEntry = ChangeTracker.Entries<ClassStudent>().FirstOrDefault(e => e.Entity.ClassStudentId == classStudentId);
            if (csEntry != null) return csEntry.Entity.CourseEnrollmentId;

            var classStudent = await ClassStudents.AsNoTracking().FirstOrDefaultAsync(cs => cs.ClassStudentId == classStudentId, cancellationToken);
            if (classStudent != null) return classStudent.CourseEnrollmentId;
        }

        int subjectResultId = 0;
        if (entry.Entity is AssessmentResult asr) subjectResultId = GetOriginalOrCurrent(entry, asr.SubjectResultId, nameof(AssessmentResult.SubjectResultId));
        else if (entry.Entity is PracticalChecklistResult pcr) subjectResultId = GetOriginalOrCurrent(entry, pcr.SubjectResultId, nameof(PracticalChecklistResult.SubjectResultId));
        else if (entry.Entity is EvidenceFile ef) subjectResultId = GetOriginalOrCurrent(entry, ef.SubjectResultId, nameof(EvidenceFile.SubjectResultId));
        else if (entry.Entity is RetakeHistory rh) subjectResultId = GetOriginalOrCurrent(entry, rh.SubjectResultId, nameof(RetakeHistory.SubjectResultId));
        else if (entry.Entity is SubjectSignoff ss) subjectResultId = GetOriginalOrCurrent(entry, ss.SubjectResultId, nameof(SubjectSignoff.SubjectResultId));

        if (subjectResultId > 0)
        {
            var srEntry = ChangeTracker.Entries<SubjectResult>().FirstOrDefault(e => e.Entity.SubjectResultId == subjectResultId);
            int resolvedEtrId = srEntry != null ? srEntry.Entity.EtrId : 0;
            
            if (resolvedEtrId == 0)
            {
                var result = await SubjectResults.AsNoTracking().FirstOrDefaultAsync(sr => sr.SubjectResultId == subjectResultId, cancellationToken);
                resolvedEtrId = result?.EtrId ?? 0;
            }

            if (resolvedEtrId > 0)
            {
                var etrEntry = ChangeTracker.Entries<ETRCourseRecord>().FirstOrDefault(e => e.Entity.ETRCourseRecordId == resolvedEtrId);
                if (etrEntry != null) return etrEntry.Entity.EnrollmentId;

                var etrResult = await ETRCourseRecords.AsNoTracking().FirstOrDefaultAsync(e => e.ETRCourseRecordId == resolvedEtrId, cancellationToken);
                if (etrResult != null) return etrResult.EnrollmentId;
            }
        }

        return 0;
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
                null, 
                entry.State == EntityState.Modified
                    && entry.Property(nameof(BaseEntity.IsDeleted)).IsModified
                    && entry.Entity.IsDeleted
                    ? "Soft delete"
                    : null))
            .ToList();
    }

    private static int? ResolveAuditUserId(EntityEntry<BaseEntity> entry)
    {
        if (entry.State == EntityState.Modified && entry.Entity.UpdatedByAccountId.HasValue)
        {
            return entry.Entity.UpdatedByAccountId;
        }

        return entry.Entity.CreatedByAccountId;
    }

    private static int GetPrimaryKeyValue(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey()
            ?? throw new InvalidOperationException($"Entity {entry.Metadata.Name} has no primary key.");

        if (key.Properties.Count > 1) return 0; 

        var keyProperty = key.Properties[0];
        var keyValue = entry.Property(keyProperty.Name).CurrentValue;

        return keyValue switch
        {
            int intValue => intValue,
            long longValue when longValue <= int.MaxValue => (int)longValue,
            _ => 0
        };
    }

    private static string? SerializePropertyValues(PropertyValues? values)
    {
        if (values is null) return null;

        var payload = values.Properties.ToDictionary(
            property => property.Name,
            property => values[property.Name]);

        return JsonSerializer.Serialize(payload, AuditJsonOptions);
    }

    private sealed record EtrRecordContext(int ETRCourseRecordId, string Status, bool IsLocked);

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
                AccountId = _userId,
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
