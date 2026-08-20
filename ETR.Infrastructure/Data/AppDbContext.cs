using System.Linq.Expressions;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ETR.Infrastructure.Data;

public partial class AppDbContext : DbContext
{
    private readonly ICurrentUserService? _currentUserService;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService? currentUserService = null)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<EvidenceType> EvidenceTypes => Set<EvidenceType>();

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<CourseSubject> CourseSubjects => Set<CourseSubject>();
    public DbSet<Class> Classes => Set<Class>();
    public DbSet<ClassSubject> ClassSubjects => Set<ClassSubject>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<CourseEnrollment> CourseEnrollments => Set<CourseEnrollment>();
    public DbSet<ETRCourseRecord> ETRCourseRecords => Set<ETRCourseRecord>();
    public DbSet<SubjectResult> SubjectResults => Set<SubjectResult>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<AssessmentResult> AssessmentResults => Set<AssessmentResult>();
    public DbSet<PracticalChecklist> PracticalChecklists => Set<PracticalChecklist>();
    public DbSet<PracticalChecklistResult> PracticalChecklistResults => Set<PracticalChecklistResult>();
    public DbSet<SubjectSignoff> SubjectSignoffs => Set<SubjectSignoff>();
    public DbSet<RetakeHistory> RetakeHistories => Set<RetakeHistory>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<EvidenceFile> EvidenceFiles => Set<EvidenceFile>();
    public DbSet<CompletionRequirement> CompletionRequirements => Set<CompletionRequirement>();
    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();
    public DbSet<ApprovalHistory> ApprovalHistories => Set<ApprovalHistory>();
    public DbSet<AmendmentRequest> AmendmentRequests => Set<AmendmentRequest>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ExportJob> ExportJobs => Set<ExportJob>();
    public DbSet<Attachment> Attachments => Set<Attachment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureKeys(modelBuilder);
        ConfigureUniqueConstraints(modelBuilder);
        ConfigureDecimalPrecision(modelBuilder);
        ConfigureRelationships(modelBuilder);
        ConfigureSoftDeleteFilters(modelBuilder);
        ConfigureEnumConversions(modelBuilder);
    }

    // Every Status-like field that used to be a raw string is now a C# enum for type safety, but the
    // DB column stays nvarchar (HasConversion<string>()) — no migration, no data rewrite, existing rows
    // (and hand-written SQL in Deploy_ETR_System.sql / raw queries) keep reading/writing the same text.
    private static void ConfigureEnumConversions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Course>().Property(c => c.Status).HasConversion<string>();
        modelBuilder.Entity<Subject>().Property(s => s.Status).HasConversion<string>();
        modelBuilder.Entity<Class>().Property(c => c.Status).HasConversion<string>();
        modelBuilder.Entity<Account>().Property(a => a.Status).HasConversion<string>();
        modelBuilder.Entity<ETRCourseRecord>().Property(e => e.Status).HasConversion<string>();
        modelBuilder.Entity<CourseEnrollment>().Property(e => e.Status).HasConversion<string>();
        modelBuilder.Entity<SubjectResult>().Property(sr => sr.Status).HasConversion<string>();
        modelBuilder.Entity<ExportJob>().Property(ej => ej.Status).HasConversion<string>();
        modelBuilder.Entity<AttendanceRecord>().Property(ar => ar.Status).HasConversion<string>();
        modelBuilder.Entity<AmendmentRequest>().Property(a => a.Status).HasConversion<string>();
        modelBuilder.Entity<UserProfile>().Property(u => u.Status).HasConversion<string>();
    }

    private static void ConfigureKeys(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasKey(e => e.RoleId);
        modelBuilder.Entity<Department>().HasKey(e => e.DepartmentId);
        modelBuilder.Entity<Course>().HasKey(e => e.CourseId);
        modelBuilder.Entity<EvidenceType>().HasKey(e => e.EvidenceTypeId);

        modelBuilder.Entity<Account>().HasKey(e => e.AccountId);
        modelBuilder.Entity<UserProfile>().HasKey(e => e.AccountId);

        modelBuilder.Entity<Subject>().HasKey(e => e.SubjectId);
        modelBuilder.Entity<CourseSubject>().HasKey(e => new { e.CourseId, e.SubjectId });
        modelBuilder.Entity<Class>().HasKey(e => e.ClassId);
        modelBuilder.Entity<ClassSubject>().HasKey(e => e.ClassSubjectId);
        modelBuilder.Entity<Session>().HasKey(e => e.SessionId);
        modelBuilder.Entity<CourseEnrollment>().HasKey(e => e.EnrollmentId);
        modelBuilder.Entity<ETRCourseRecord>().HasKey(e => e.ETRCourseRecordId);
        modelBuilder.Entity<SubjectResult>().HasKey(e => e.SubjectResultId);
        modelBuilder.Entity<Assessment>().HasKey(e => e.AssessmentId);
        modelBuilder.Entity<AssessmentResult>().HasKey(e => e.AssessmentResultId);
        modelBuilder.Entity<PracticalChecklist>().HasKey(e => e.PracticalChecklistId);
        modelBuilder.Entity<PracticalChecklistResult>().HasKey(e => e.PracticalChecklistResultId);
        modelBuilder.Entity<SubjectSignoff>().HasKey(e => e.SubjectSignoffId);
        modelBuilder.Entity<RetakeHistory>().HasKey(e => e.RetakeHistoryId);
        modelBuilder.Entity<AttendanceRecord>().HasKey(e => e.AttendanceRecordId);
        modelBuilder.Entity<EvidenceFile>().HasKey(e => e.EvidenceFileId);
        modelBuilder.Entity<CompletionRequirement>().HasKey(e => e.RequirementId);
        modelBuilder.Entity<ApprovalRequest>().HasKey(e => e.ApprovalRequestId);
        modelBuilder.Entity<ApprovalHistory>().HasKey(e => e.ApprovalHistoryId);
        modelBuilder.Entity<AuditLog>().HasKey(e => e.AuditLogId);
        modelBuilder.Entity<ExportJob>().HasKey(e => e.ExportJobId);
        modelBuilder.Entity<Attachment>().HasKey(e => e.AttachmentId);
    }

    private static void ConfigureUniqueConstraints(ModelBuilder modelBuilder)
    {
        // Filtered WHERE IsDeleted = 0 on every unique index below: BaseEntity soft-delete is
        // global (ConfigureSoftDeleteFilters), so a raw unique index would still block reusing a
        // key after the row is soft-deleted. Filtering keeps uniqueness scoped to active rows only.
        modelBuilder.Entity<Role>().HasIndex(r => r.RoleName).IsUnique().HasFilter("[IsDeleted] = 0");
        modelBuilder.Entity<Department>().HasIndex(d => d.DepartmentName).IsUnique().HasFilter("[IsDeleted] = 0");
        modelBuilder.Entity<EvidenceType>().HasIndex(et => et.TypeName).IsUnique().HasFilter("[IsDeleted] = 0");
        modelBuilder.Entity<Course>().HasIndex(c => c.CourseCode).IsUnique().HasFilter("[IsDeleted] = 0");
        modelBuilder.Entity<Subject>().HasIndex(s => s.SubjectCode).IsUnique().HasFilter("[IsDeleted] = 0");
        modelBuilder.Entity<Class>().HasIndex(tc => tc.ClassCode).IsUnique().HasFilter("[IsDeleted] = 0");

        modelBuilder.Entity<Account>().HasIndex(u => u.Username).IsUnique().HasFilter("[IsDeleted] = 0");
        modelBuilder.Entity<UserProfile>().HasIndex(u => u.Email).IsUnique().HasFilter("[Email] IS NOT NULL AND [Email] <> '' AND [IsDeleted] = 0");

        modelBuilder.Entity<CourseEnrollment>()
            .HasIndex(e => new { e.AccountId, e.ClassId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        modelBuilder.Entity<ETRCourseRecord>()
            .HasIndex(e => e.EnrollmentId)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        modelBuilder.Entity<SubjectResult>()
            .HasIndex(sr => new { sr.EtrId, sr.CourseId, sr.SubjectId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        modelBuilder.Entity<ClassSubject>()
            .HasIndex(cs => new { cs.ClassId, cs.SubjectId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        modelBuilder.Entity<AttendanceRecord>()
            .HasIndex(ar => new { ar.SessionId, ar.EnrollmentId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // AttemptNo trong khoá unique — cho phép mỗi lần retake tạo 1 dòng mới (giữ lịch sử điểm),
        // thay vì ghi đè dòng cũ; vẫn chặn 2 dòng cùng tuyên bố cùng 1 attempt number.
        modelBuilder.Entity<AssessmentResult>()
            .HasIndex(ar => new { ar.AssessmentId, ar.AccountId, ar.SessionId, ar.AttemptNo })
            .IsUnique()
            .HasFilter("[SessionId] IS NOT NULL AND [IsDeleted] = 0");

        modelBuilder.Entity<PracticalChecklistResult>()
            .HasIndex(pcr => new { pcr.SubjectResultId, pcr.PracticalChecklistId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Non-unique — one owner can have more than one attachment (e.g. a future gallery-style
        // owner type). No FK to any single table on purpose: OwnerType/OwnerId is a polymorphic
        // association that can point at ANY entity, which EF Core cannot express as a real FK.
        modelBuilder.Entity<Attachment>()
            .HasIndex(a => new { a.OwnerType, a.OwnerId });
    }

    private static void ConfigureDecimalPrecision(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CourseSubject>().Property(cs => cs.PassingScore).HasColumnType("decimal(5,2)");
        modelBuilder.Entity<SubjectResult>().Property(sr => sr.AttendanceRate).HasColumnType("decimal(5,2)");
        modelBuilder.Entity<SubjectResult>().Property(sr => sr.Score).HasColumnType("decimal(5,2)");
        modelBuilder.Entity<Assessment>().Property(a => a.Weight).HasColumnType("decimal(5,2)");
        modelBuilder.Entity<Assessment>().Property(a => a.PassingScore).HasColumnType("decimal(5,2)");
        modelBuilder.Entity<AssessmentResult>().Property(a => a.Score).HasColumnType("decimal(5,2)");
        modelBuilder.Entity<AssessmentResult>().Property(a => a.PassingScoreSnapshot).HasColumnType("decimal(5,2)");
        modelBuilder.Entity<AssessmentResult>().Property(a => a.WeightSnapshot).HasColumnType("decimal(5,2)");
        modelBuilder.Entity<SubjectResult>().Property(sr => sr.PassingScoreSnapshot).HasColumnType("decimal(5,2)");
        modelBuilder.Entity<RetakeHistory>().Property(rh => rh.PreviousScore).HasColumnType("decimal(5,2)");
        modelBuilder.Entity<RetakeHistory>().Property(rh => rh.NewScore).HasColumnType("decimal(5,2)");
        modelBuilder.Entity<PracticalChecklistResult>().Property(p => p.Score).HasColumnType("decimal(5,2)");
        modelBuilder.Entity<CompletionRequirement>().Property(c => c.ThresholdValue).HasColumnType("decimal(5,2)");
    }

    private static void ConfigureRelationships(ModelBuilder modelBuilder)
    {
        var cascadeDeleteConfig = DeleteBehavior.Restrict;

        // Identity Setup
        modelBuilder.Entity<Account>()
            .HasOne(a => a.Profile)
            .WithOne(up => up.Account)
            .HasForeignKey<UserProfile>(up => up.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Account>().HasOne<Role>().WithMany().HasForeignKey(u => u.RoleId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<Account>().HasOne<Department>().WithMany().HasForeignKey(u => u.DepartmentId).OnDelete(cascadeDeleteConfig);

        // Course & Class Setup
        modelBuilder.Entity<CourseSubject>().HasOne<Course>().WithMany().HasForeignKey(cs => cs.CourseId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<CourseSubject>().HasOne<Subject>().WithMany().HasForeignKey(cs => cs.SubjectId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<Class>().HasOne<Course>().WithMany().HasForeignKey(tc => tc.CourseId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<ClassSubject>().HasOne<Class>().WithMany().HasForeignKey(cs => cs.ClassId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<ClassSubject>().HasOne<Subject>().WithMany().HasForeignKey(cs => cs.SubjectId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<ClassSubject>().HasOne<Account>().WithMany().HasForeignKey(cs => cs.InstructorAccountId).OnDelete(cascadeDeleteConfig);

        // Enrollment
        modelBuilder.Entity<CourseEnrollment>().HasOne<Account>().WithMany().HasForeignKey(e => e.AccountId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<CourseEnrollment>().HasOne<Class>().WithMany().HasForeignKey(e => e.ClassId).OnDelete(cascadeDeleteConfig);

        // ETR Restructure
        modelBuilder.Entity<ETRCourseRecord>().HasOne<CourseEnrollment>().WithOne().HasForeignKey<ETRCourseRecord>(e => e.EnrollmentId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<SubjectResult>().HasOne<ETRCourseRecord>().WithMany(e => e.SubjectResults).HasForeignKey(sr => sr.EtrId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<SubjectResult>().HasOne<CourseSubject>().WithMany().HasForeignKey(sr => new { sr.CourseId, sr.SubjectId }).OnDelete(cascadeDeleteConfig);

        // Session Setup
        modelBuilder.Entity<Session>().HasOne<Class>().WithMany().HasForeignKey(s => s.ClassId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<Session>().HasOne<Subject>().WithMany().HasForeignKey(s => s.SubjectId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<Session>().HasOne<Account>().WithMany().HasForeignKey(s => s.ConfirmedByAccountId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<Session>().HasOne<Assessment>().WithMany().HasForeignKey(s => s.AssessmentId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<Session>().HasOne<PracticalChecklist>().WithMany().HasForeignKey(s => s.PracticalChecklistId).OnDelete(cascadeDeleteConfig);

        // Attendance Setup
        modelBuilder.Entity<AttendanceRecord>().HasOne<Session>().WithMany().HasForeignKey(ar => ar.SessionId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<AttendanceRecord>().HasOne<CourseEnrollment>().WithMany().HasForeignKey(ar => ar.EnrollmentId).OnDelete(cascadeDeleteConfig);

        // Assessment Setup
        modelBuilder.Entity<Assessment>().HasOne<CourseSubject>().WithMany().HasForeignKey(a => new { a.CourseId, a.SubjectId }).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<AssessmentResult>().HasOne<Assessment>().WithMany().HasForeignKey(ar => ar.AssessmentId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<AssessmentResult>().HasOne<Account>().WithMany().HasForeignKey(ar => ar.AccountId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<AssessmentResult>().HasOne<SubjectResult>().WithMany().HasForeignKey(ar => ar.SubjectResultId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<AssessmentResult>().HasOne<Account>().WithMany().HasForeignKey(ar => ar.GradedByAccountId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<AssessmentResult>().HasOne(ar => ar.Session).WithMany().HasForeignKey(ar => ar.SessionId).OnDelete(cascadeDeleteConfig);

        // Practical Checklist Setup
        modelBuilder.Entity<PracticalChecklist>().HasOne<CourseSubject>().WithMany().HasForeignKey(pc => new { pc.CourseId, pc.SubjectId }).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<PracticalChecklistResult>().HasOne<PracticalChecklist>().WithMany().HasForeignKey(pcr => pcr.PracticalChecklistId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<PracticalChecklistResult>().HasOne<SubjectResult>().WithMany().HasForeignKey(pcr => pcr.SubjectResultId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<PracticalChecklistResult>().HasOne<Account>().WithMany().HasForeignKey(pcr => pcr.VerifiedByAccountId).OnDelete(cascadeDeleteConfig);

        // Signoff & Retake Setup
        modelBuilder.Entity<SubjectSignoff>().HasOne<SubjectResult>().WithMany().HasForeignKey(ss => ss.SubjectResultId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<SubjectSignoff>().HasOne<Account>().WithMany().HasForeignKey(ss => ss.SignoffByAccountId).OnDelete(cascadeDeleteConfig);

        modelBuilder.Entity<RetakeHistory>().HasOne<SubjectResult>().WithMany().HasForeignKey(rh => rh.SubjectResultId).OnDelete(cascadeDeleteConfig);

        // Evidence Setup
        modelBuilder.Entity<EvidenceFile>().HasOne<EvidenceType>().WithMany().HasForeignKey(ef => ef.EvidenceTypeId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<EvidenceFile>().HasOne<Account>().WithMany().HasForeignKey(ef => ef.AccountId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<EvidenceFile>().HasOne<Account>().WithMany().HasForeignKey(ef => ef.UploadedByAccountId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<EvidenceFile>().HasOne<Account>().WithMany().HasForeignKey(ef => ef.VerifiedByAccountId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<EvidenceFile>().HasOne<SubjectResult>().WithMany().HasForeignKey(ef => ef.SubjectResultId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<EvidenceFile>().HasOne<AttendanceRecord>().WithMany().HasForeignKey(ef => ef.AttendanceRecordId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<EvidenceFile>().HasOne<AssessmentResult>().WithMany().HasForeignKey(ef => ef.AssessmentResultId).OnDelete(cascadeDeleteConfig);

        // Approval Request Setup
        modelBuilder.Entity<ApprovalRequest>().HasOne<ETRCourseRecord>().WithMany().HasForeignKey(ar => ar.ETRCourseRecordId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<ApprovalHistory>().HasOne<ApprovalRequest>().WithMany().HasForeignKey(ah => ah.ApprovalRequestId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<ApprovalHistory>().HasOne<Account>().WithMany().HasForeignKey(ah => ah.ActionByAccountId).OnDelete(cascadeDeleteConfig);

        // ExportJob
        modelBuilder.Entity<ExportJob>().HasOne<Account>().WithMany().HasForeignKey(ej => ej.RequestedByAccountId).OnDelete(cascadeDeleteConfig);
    }

    private static void ConfigureSoftDeleteFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(
                    CreateIsDeletedFilter(entityType.ClrType));
            }
        }
    }

    private static LambdaExpression CreateIsDeletedFilter(Type entityType)
    {
        var parameter = Expression.Parameter(entityType, "entity");
        var isDeletedProperty = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
        var filter = Expression.Equal(isDeletedProperty, Expression.Constant(false));

        return Expression.Lambda(filter, parameter);
    }
}
