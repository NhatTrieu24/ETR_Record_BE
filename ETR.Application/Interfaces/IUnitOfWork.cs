using ETR.Domain.Entities;

namespace ETR.Application.Interfaces;

public interface IUnitOfWork
{
    IGenericRepository<Account> AccountRepository { get; }
    IGenericRepository<UserProfile> UserProfileRepository { get; }
    IGenericRepository<ClassStudent> ClassStudentRepository { get; }
    IGenericRepository<CourseEnrollment> CourseEnrollmentRepository { get; }
    IETRCourseRecordRepository ETRCourseRecordRepository { get; }
    IGenericRepository<Class> ClassRepository { get; }
    IGenericRepository<Subject> SubjectRepository { get; }
    IGenericRepository<CourseSubject> CourseSubjectRepository { get; }
    IGenericRepository<Role> RoleRepository { get; }
    IGenericRepository<Department> DepartmentRepository { get; }
    IGenericRepository<Course> CourseRepository { get; }
    IGenericRepository<EvidenceType> EvidenceTypeRepository { get; }
    IGenericRepository<Session> SessionRepository { get; }
    IGenericRepository<SubjectResult> SubjectResultRepository { get; }
    IGenericRepository<AttendanceRecord> AttendanceRecordRepository { get; }
    IGenericRepository<Assessment> AssessmentRepository { get; }
    IGenericRepository<AssessmentResult> AssessmentResultRepository { get; }
    IGenericRepository<PracticalChecklist> PracticalChecklistRepository { get; }
    IGenericRepository<PracticalChecklistResult> PracticalChecklistResultRepository { get; }
    IGenericRepository<SubjectSignoff> SubjectSignoffRepository { get; }
    IGenericRepository<RetakeHistory> RetakeHistoryRepository { get; }
    IGenericRepository<EvidenceFile> EvidenceFileRepository { get; }
    IGenericRepository<CompletionRequirement> CompletionRequirementRepository { get; }
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
