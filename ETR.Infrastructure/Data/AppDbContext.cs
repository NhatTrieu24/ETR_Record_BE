using System.Linq.Expressions;
using ETR.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ETR.Infrastructure.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<LearnerType> LearnerTypes => Set<LearnerType>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<EvidenceType> EvidenceTypes => Set<EvidenceType>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Learner> Learners => Set<Learner>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<CourseSubject> CourseSubjects => Set<CourseSubject>();
    public DbSet<Class> Classes => Set<Class>();
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
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ExportJob> ExportJobs => Set<ExportJob>();
    public DbSet<DashboardSnapshot> DashboardSnapshots => Set<DashboardSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureKeys(modelBuilder);
        ConfigureUniqueConstraints(modelBuilder);
        ConfigureDecimalPrecision(modelBuilder);
        ConfigureRelationships(modelBuilder);
        ConfigureSoftDeleteFilters(modelBuilder);
    }

    private static void ConfigureKeys(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasKey(e => e.RoleId);
        modelBuilder.Entity<Department>().HasKey(e => e.DepartmentId);
        modelBuilder.Entity<LearnerType>().HasKey(e => e.LearnerTypeId);
        modelBuilder.Entity<Course>().HasKey(e => e.CourseId);
        modelBuilder.Entity<EvidenceType>().HasKey(e => e.EvidenceTypeId);
        modelBuilder.Entity<User>().HasKey(e => e.UserId);
        modelBuilder.Entity<Learner>().HasKey(e => e.LearnerId);
        modelBuilder.Entity<Subject>().HasKey(e => e.SubjectId);
        modelBuilder.Entity<CourseSubject>().HasKey(e => new { e.CourseId, e.SubjectId });
        modelBuilder.Entity<Class>().HasKey(e => e.ClassId);
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
        modelBuilder.Entity<DashboardSnapshot>().HasKey(e => e.SnapshotId);
    }

    private static void ConfigureUniqueConstraints(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasIndex(r => r.RoleName).IsUnique();
        modelBuilder.Entity<Department>().HasIndex(d => d.DepartmentName).IsUnique();
        modelBuilder.Entity<LearnerType>().HasIndex(lt => lt.TypeName).IsUnique();
        modelBuilder.Entity<EvidenceType>().HasIndex(et => et.TypeName).IsUnique();
        modelBuilder.Entity<Course>().HasIndex(c => c.CourseCode).IsUnique();
        modelBuilder.Entity<Subject>().HasIndex(s => s.SubjectCode).IsUnique();
        modelBuilder.Entity<Class>().HasIndex(tc => tc.ClassCode).IsUnique();
        modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<Learner>().HasIndex(l => l.LearnerCode).IsUnique();
        modelBuilder.Entity<Learner>().HasIndex(l => l.Email).IsUnique().HasFilter("[Email] IS NOT NULL");
        modelBuilder.Entity<Learner>().HasIndex(l => l.IdentificationNumber).IsUnique().HasFilter("[IdentificationNumber] IS NOT NULL");

        modelBuilder.Entity<CourseEnrollment>()
            .HasIndex(e => new { e.LearnerId, e.ClassId })
            .IsUnique();

        modelBuilder.Entity<ETRCourseRecord>()
            .HasIndex(e => e.EnrollmentId)
            .IsUnique();

        modelBuilder.Entity<SubjectResult>()
            .HasIndex(sr => new { sr.EnrollmentId, sr.CourseId, sr.SubjectId })
            .IsUnique();

        modelBuilder.Entity<AttendanceRecord>()
            .HasIndex(ar => new { ar.SessionId, ar.LearnerId })
            .IsUnique();

        modelBuilder.Entity<AssessmentResult>()
            .HasIndex(ar => new { ar.AssessmentId, ar.LearnerId })
            .IsUnique();
            
        modelBuilder.Entity<PracticalChecklistResult>()
            .HasIndex(pcr => new { pcr.SubjectResultId, pcr.PracticalChecklistId })
            .IsUnique();
    }

    private static void ConfigureDecimalPrecision(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CourseSubject>().Property(cs => cs.PassingScore).HasColumnType("decimal(5,2)");
        modelBuilder.Entity<SubjectResult>().Property(sr => sr.AttendanceRate).HasColumnType("decimal(5,2)");
        modelBuilder.Entity<SubjectResult>().Property(sr => sr.Score).HasColumnType("decimal(5,2)");
        modelBuilder.Entity<Assessment>().Property(a => a.Weight).HasColumnType("decimal(5,2)");
        modelBuilder.Entity<Assessment>().Property(a => a.PassingScore).HasColumnType("decimal(5,2)");
        modelBuilder.Entity<AssessmentResult>().Property(a => a.Score).HasColumnType("decimal(5,2)");
        modelBuilder.Entity<RetakeHistory>().Property(rh => rh.PreviousScore).HasColumnType("decimal(5,2)");
        modelBuilder.Entity<RetakeHistory>().Property(rh => rh.NewScore).HasColumnType("decimal(5,2)");
        modelBuilder.Entity<DashboardSnapshot>().Property(d => d.AverageAttendanceRate).HasColumnType("decimal(5,2)");
        modelBuilder.Entity<DashboardSnapshot>().Property(d => d.AverageAssessmentScore).HasColumnType("decimal(5,2)");
    }

    private static void ConfigureRelationships(ModelBuilder modelBuilder)
    {
        var cascadeDeleteConfig = DeleteBehavior.Restrict;

        // User & Learner Setup
        modelBuilder.Entity<User>().HasOne<Role>().WithMany().HasForeignKey(u => u.RoleId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<User>().HasOne<Department>().WithMany().HasForeignKey(u => u.DepartmentId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<Learner>().HasOne<LearnerType>().WithMany().HasForeignKey(l => l.LearnerTypeId).OnDelete(cascadeDeleteConfig);

        // Course & Class Setup
        modelBuilder.Entity<CourseSubject>().HasOne<Course>().WithMany().HasForeignKey(cs => cs.CourseId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<CourseSubject>().HasOne<Subject>().WithMany().HasForeignKey(cs => cs.SubjectId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<Class>().HasOne<Course>().WithMany().HasForeignKey(tc => tc.CourseId).OnDelete(cascadeDeleteConfig);
        
        // Enrollment Setup
        modelBuilder.Entity<CourseEnrollment>().HasOne<Learner>().WithMany().HasForeignKey(e => e.LearnerId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<CourseEnrollment>().HasOne<Class>().WithMany().HasForeignKey(e => e.ClassId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<ETRCourseRecord>().HasOne<CourseEnrollment>().WithOne().HasForeignKey<ETRCourseRecord>(e => e.EnrollmentId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<SubjectResult>().HasOne<CourseEnrollment>().WithMany().HasForeignKey(sr => sr.EnrollmentId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<SubjectResult>().HasOne<CourseSubject>().WithMany().HasForeignKey(sr => new { sr.CourseId, sr.SubjectId }).OnDelete(cascadeDeleteConfig);

        // Session Setup
        modelBuilder.Entity<Session>().HasOne<Class>().WithMany().HasForeignKey(s => s.ClassId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<Session>().HasOne<Subject>().WithMany().HasForeignKey(s => s.SubjectId).OnDelete(cascadeDeleteConfig);

        // Attendance Setup
        modelBuilder.Entity<AttendanceRecord>().HasOne<Session>().WithMany().HasForeignKey(ar => ar.SessionId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<AttendanceRecord>().HasOne<Learner>().WithMany().HasForeignKey(ar => ar.LearnerId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<AttendanceRecord>().HasOne<CourseEnrollment>().WithMany().HasForeignKey(ar => ar.EnrollmentId).OnDelete(cascadeDeleteConfig);

        // Assessment Setup
        modelBuilder.Entity<Assessment>().HasOne<CourseSubject>().WithMany().HasForeignKey(a => new { a.CourseId, a.SubjectId }).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<AssessmentResult>().HasOne<Assessment>().WithMany().HasForeignKey(ar => ar.AssessmentId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<AssessmentResult>().HasOne<Learner>().WithMany().HasForeignKey(ar => ar.LearnerId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<AssessmentResult>().HasOne<SubjectResult>().WithMany().HasForeignKey(ar => ar.SubjectResultId).OnDelete(cascadeDeleteConfig);

        // Practical Checklist Setup
        modelBuilder.Entity<PracticalChecklist>().HasOne<CourseSubject>().WithMany().HasForeignKey(pc => new { pc.CourseId, pc.SubjectId }).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<PracticalChecklistResult>().HasOne<PracticalChecklist>().WithMany().HasForeignKey(pcr => pcr.PracticalChecklistId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<PracticalChecklistResult>().HasOne<SubjectResult>().WithMany().HasForeignKey(pcr => pcr.SubjectResultId).OnDelete(cascadeDeleteConfig);

        // Signoff & Retake Setup
        modelBuilder.Entity<SubjectSignoff>().HasOne<SubjectResult>().WithMany().HasForeignKey(ss => ss.SubjectResultId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<RetakeHistory>().HasOne<SubjectResult>().WithMany().HasForeignKey(rh => rh.SubjectResultId).OnDelete(cascadeDeleteConfig);

        // Evidence Setup
        modelBuilder.Entity<EvidenceFile>().HasOne<EvidenceType>().WithMany().HasForeignKey(ef => ef.EvidenceTypeId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<EvidenceFile>().HasOne<Learner>().WithMany().HasForeignKey(ef => ef.LearnerId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<EvidenceFile>().HasOne<SubjectResult>().WithMany().HasForeignKey(ef => ef.SubjectResultId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<EvidenceFile>().HasOne<AttendanceRecord>().WithMany().HasForeignKey(ef => ef.AttendanceRecordId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<EvidenceFile>().HasOne<AssessmentResult>().WithMany().HasForeignKey(ef => ef.AssessmentResultId).OnDelete(cascadeDeleteConfig);

        // Approval Request Setup
        modelBuilder.Entity<ApprovalRequest>().HasOne<ETRCourseRecord>().WithMany().HasForeignKey(ar => ar.ETRCourseRecordId).OnDelete(cascadeDeleteConfig);
        modelBuilder.Entity<ApprovalHistory>().HasOne<ApprovalRequest>().WithMany().HasForeignKey(ah => ah.ApprovalRequestId).OnDelete(cascadeDeleteConfig);
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
