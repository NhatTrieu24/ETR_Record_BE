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
    public DbSet<TrainingClass> TrainingClasses => Set<TrainingClass>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<ClassInstructor> ClassInstructors => Set<ClassInstructor>();
    public DbSet<CompletionRequirement> CompletionRequirements => Set<CompletionRequirement>();
    public DbSet<ETRChecklistTemplate> ETRChecklistTemplates => Set<ETRChecklistTemplate>();
    public DbSet<ETRChecklistItem> ETRChecklistItems => Set<ETRChecklistItem>();
    public DbSet<ETRRecord> ETRRecords => Set<ETRRecord>();
    public DbSet<ETRChecklistProgress> ETRChecklistProgresses => Set<ETRChecklistProgress>();
    public DbSet<AttendanceSession> AttendanceSessions => Set<AttendanceSession>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<AssessmentComponent> AssessmentComponents => Set<AssessmentComponent>();
    public DbSet<AssessmentResult> AssessmentResults => Set<AssessmentResult>();
    public DbSet<EvidenceFile> EvidenceFiles => Set<EvidenceFile>();
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
        modelBuilder.Entity<TrainingClass>().HasKey(e => e.ClassId);
        modelBuilder.Entity<Enrollment>().HasKey(e => e.EnrollmentId);
        modelBuilder.Entity<ClassInstructor>().HasKey(e => e.ClassInstructorId);
        modelBuilder.Entity<CompletionRequirement>().HasKey(e => e.RequirementId);
        modelBuilder.Entity<ETRChecklistTemplate>().HasKey(e => e.TemplateId);
        modelBuilder.Entity<ETRChecklistItem>().HasKey(e => e.ChecklistItemId);
        modelBuilder.Entity<ETRRecord>().HasKey(e => e.ETRRecordId);
        modelBuilder.Entity<ETRChecklistProgress>().HasKey(e => e.ProgressId);
        modelBuilder.Entity<AttendanceSession>().HasKey(e => e.AttendanceSessionId);
        modelBuilder.Entity<AttendanceRecord>().HasKey(e => e.AttendanceRecordId);
        modelBuilder.Entity<AssessmentComponent>().HasKey(e => e.AssessmentComponentId);
        modelBuilder.Entity<AssessmentResult>().HasKey(e => e.AssessmentResultId);
        modelBuilder.Entity<EvidenceFile>().HasKey(e => e.EvidenceFileId);
        modelBuilder.Entity<ApprovalRequest>().HasKey(e => e.ApprovalRequestId);
        modelBuilder.Entity<ApprovalHistory>().HasKey(e => e.ApprovalHistoryId);
        modelBuilder.Entity<AuditLog>().HasKey(e => e.AuditLogId);
        modelBuilder.Entity<ExportJob>().HasKey(e => e.ExportJobId);
        modelBuilder.Entity<DashboardSnapshot>().HasKey(e => e.SnapshotId);
    }

    private static void ConfigureUniqueConstraints(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>()
            .HasIndex(r => r.RoleName)
            .IsUnique();

        modelBuilder.Entity<Department>()
            .HasIndex(d => d.DepartmentName)
            .IsUnique();

        modelBuilder.Entity<LearnerType>()
            .HasIndex(lt => lt.TypeName)
            .IsUnique();

        modelBuilder.Entity<EvidenceType>()
            .HasIndex(et => et.TypeName)
            .IsUnique();

        modelBuilder.Entity<Course>()
            .HasIndex(c => c.CourseCode)
            .IsUnique();

        modelBuilder.Entity<TrainingClass>()
            .HasIndex(tc => tc.ClassCode)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Learner>()
            .HasIndex(l => l.LearnerCode)
            .IsUnique();

        modelBuilder.Entity<Learner>()
            .HasIndex(l => l.Email)
            .IsUnique()
            .HasFilter("[Email] IS NOT NULL");

        modelBuilder.Entity<Learner>()
            .HasIndex(l => l.IdentificationNumber)
            .IsUnique()
            .HasFilter("[IdentificationNumber] IS NOT NULL");

        modelBuilder.Entity<Enrollment>()
            .HasIndex(e => new { e.LearnerId, e.ClassId })
            .IsUnique();

        modelBuilder.Entity<ClassInstructor>()
            .HasIndex(ci => new { ci.ClassId, ci.UserId })
            .IsUnique();

        modelBuilder.Entity<ETRRecord>()
            .HasIndex(e => e.EnrollmentId)
            .IsUnique();

        modelBuilder.Entity<ETRChecklistProgress>()
            .HasIndex(p => new { p.ETRRecordId, p.ChecklistItemId })
            .IsUnique();

        modelBuilder.Entity<AttendanceRecord>()
            .HasIndex(ar => new { ar.AttendanceSessionId, ar.LearnerId })
            .IsUnique();

        modelBuilder.Entity<AssessmentResult>()
            .HasIndex(ar => new { ar.AssessmentComponentId, ar.LearnerId })
            .IsUnique();
    }

    private static void ConfigureDecimalPrecision(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AssessmentComponent>()
            .Property(a => a.Weight)
            .HasColumnType("decimal(5,2)");

        modelBuilder.Entity<AssessmentComponent>()
            .Property(a => a.PassingScore)
            .HasColumnType("decimal(5,2)");

        modelBuilder.Entity<AssessmentResult>()
            .Property(a => a.Score)
            .HasColumnType("decimal(5,2)");

        modelBuilder.Entity<DashboardSnapshot>()
            .Property(d => d.AverageAttendanceRate)
            .HasColumnType("decimal(5,2)");

        modelBuilder.Entity<DashboardSnapshot>()
            .Property(d => d.AverageAssessmentScore)
            .HasColumnType("decimal(5,2)");
    }

    private static void ConfigureRelationships(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasOne<Role>()
            .WithMany()
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasOne<Department>()
            .WithMany()
            .HasForeignKey(u => u.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Learner>()
            .HasOne<LearnerType>()
            .WithMany()
            .HasForeignKey(l => l.LearnerTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TrainingClass>()
            .HasOne<Course>()
            .WithMany()
            .HasForeignKey(tc => tc.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Enrollment>()
            .HasOne<Learner>()
            .WithMany()
            .HasForeignKey(e => e.LearnerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Enrollment>()
            .HasOne<TrainingClass>()
            .WithMany()
            .HasForeignKey(e => e.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Enrollment>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ClassInstructor>()
            .HasOne<TrainingClass>()
            .WithMany()
            .HasForeignKey(ci => ci.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ClassInstructor>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(ci => ci.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ETRRecord>()
            .HasOne<Enrollment>()
            .WithOne()
            .HasForeignKey<ETRRecord>(e => e.EnrollmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ETRChecklistProgress>()
            .HasOne<ETRRecord>()
            .WithMany()
            .HasForeignKey(p => p.ETRRecordId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ETRChecklistProgress>()
            .HasOne<ETRChecklistItem>()
            .WithMany()
            .HasForeignKey(p => p.ChecklistItemId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ETRChecklistProgress>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.VerifiedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AttendanceSession>()
            .HasOne<TrainingClass>()
            .WithMany()
            .HasForeignKey(a => a.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AttendanceSession>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(a => a.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AttendanceSession>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(a => a.ConfirmedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AttendanceRecord>()
            .HasOne<AttendanceSession>()
            .WithMany()
            .HasForeignKey(ar => ar.AttendanceSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AttendanceRecord>()
            .HasOne<Learner>()
            .WithMany()
            .HasForeignKey(ar => ar.LearnerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AttendanceRecord>()
            .HasOne<ETRRecord>()
            .WithMany()
            .HasForeignKey(ar => ar.ETRRecordId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AttendanceRecord>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(ar => ar.RecordedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AssessmentComponent>()
            .HasOne<Course>()
            .WithMany()
            .HasForeignKey(ac => ac.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AssessmentResult>()
            .HasOne<AssessmentComponent>()
            .WithMany()
            .HasForeignKey(ar => ar.AssessmentComponentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AssessmentResult>()
            .HasOne<Learner>()
            .WithMany()
            .HasForeignKey(ar => ar.LearnerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AssessmentResult>()
            .HasOne<ETRRecord>()
            .WithMany()
            .HasForeignKey(ar => ar.ETRRecordId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AssessmentResult>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(ar => ar.RecordedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EvidenceFile>()
            .HasOne<EvidenceType>()
            .WithMany()
            .HasForeignKey(ef => ef.EvidenceTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EvidenceFile>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(ef => ef.UploadedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EvidenceFile>()
            .HasOne<Learner>()
            .WithMany()
            .HasForeignKey(ef => ef.LearnerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EvidenceFile>()
            .HasOne<ETRRecord>()
            .WithMany()
            .HasForeignKey(ef => ef.ETRRecordId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EvidenceFile>()
            .HasOne<AttendanceRecord>()
            .WithMany()
            .HasForeignKey(ef => ef.AttendanceRecordId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EvidenceFile>()
            .HasOne<AssessmentResult>()
            .WithMany()
            .HasForeignKey(ef => ef.AssessmentResultId)
            .OnDelete(DeleteBehavior.Restrict);
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
