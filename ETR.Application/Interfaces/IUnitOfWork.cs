using ETR.Domain.Entities;

namespace ETR.Application.Interfaces;

public interface IUnitOfWork
{
    IGenericRepository<User> UserRepository { get; }
    IGenericRepository<Learner> LearnerRepository { get; }
    IGenericRepository<Enrollment> EnrollmentRepository { get; }
    IGenericRepository<ETRRecord> ETRRecordRepository { get; }
    IGenericRepository<TrainingClass> TrainingClassRepository { get; }
    IGenericRepository<ETRChecklistTemplate> ETRChecklistTemplateRepository { get; }
    IGenericRepository<ETRChecklistItem> ETRChecklistItemRepository { get; }
    IGenericRepository<ETRChecklistProgress> ETRChecklistProgressRepository { get; }
    IGenericRepository<Role> RoleRepository { get; }
    IGenericRepository<Department> DepartmentRepository { get; }
    IGenericRepository<LearnerType> LearnerTypeRepository { get; }
    IGenericRepository<Course> CourseRepository { get; }
    IGenericRepository<EvidenceType> EvidenceTypeRepository { get; }
    IGenericRepository<ClassInstructor> ClassInstructorRepository { get; }
    IGenericRepository<CompletionRequirement> CompletionRequirementRepository { get; }
    IGenericRepository<AttendanceSession> AttendanceSessionRepository { get; }
    IGenericRepository<AttendanceRecord> AttendanceRecordRepository { get; }
    IGenericRepository<AssessmentComponent> AssessmentComponentRepository { get; }
    IGenericRepository<AssessmentResult> AssessmentResultRepository { get; }
    IGenericRepository<EvidenceFile> EvidenceFileRepository { get; }
    IGenericRepository<ApprovalRequest> ApprovalRequestRepository { get; }
    IGenericRepository<ApprovalHistory> ApprovalHistoryRepository { get; }
    IAuditLogRepository AuditLogRepository { get; }
    IGenericRepository<ExportJob> ExportJobRepository { get; }
    IGenericRepository<DashboardSnapshot> DashboardSnapshotRepository { get; }
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    Task<int> SaveAsync(CancellationToken cancellationToken = default);
    Task<T> ExecuteInStrategyAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);
}
