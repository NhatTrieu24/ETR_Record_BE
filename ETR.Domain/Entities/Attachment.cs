namespace ETR.Domain.Entities;

/// <summary>Polymorphic file reference — one shared table for every entity that needs to point at a
/// file hosted externally (Cloudinary), instead of a per-entity file-metadata block or a dedicated
/// table per owner type. The FE uploads directly to Cloudinary and only ever hands the backend a
/// URL; the backend never receives or writes file bytes for these.
///
/// <see cref="OwnerType"/>/<see cref="OwnerId"/> is a polymorphic association (no FK constraint is
/// possible/declared for it — the owner can be any entity type, see AppDbContext.ConfigureKeys for
/// the deliberate absence of a HasOne/WithMany here). Populate OwnerType with <c>nameof(TOwner)</c>
/// so it always matches the CLR type name driving the rest of the codebase's EntityName conventions
/// (AuditLog.EntityName does the same).</summary>
public class Attachment : BaseEntity
{
    public int AttachmentId { get; set; }
    public string OwnerType { get; set; } = string.Empty;
    public int OwnerId { get; set; }

    /// <summary>Cloudinary secure_url (or any external file host URL) — the only place a file's
    /// bytes are resolvable from; this backend never stores them.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Cloudinary public_id, kept for a future "delete from Cloudinary too" admin action.
    /// Null for any non-Cloudinary URL.</summary>
    public string? PublicId { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string? MimeType { get; set; }
    public long? FileSize { get; set; }

    public int UploadedByAccountId { get; set; }
    public DateTime UploadedAt { get; set; }
}
