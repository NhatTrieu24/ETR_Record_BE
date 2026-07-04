using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using ETR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ETR.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
        UserRepository = new GenericRepository<User>(_context);
        LearnerRepository = new GenericRepository<Learner>(_context);
        CourseEnrollmentRepository = new GenericRepository<CourseEnrollment>(_context);
        ETRCourseRecordRepository = new GenericRepository<ETRCourseRecord>(_context);
        ClassRepository = new GenericRepository<Class>(_context);
        SubjectRepository = new GenericRepository<Subject>(_context);
        CourseSubjectRepository = new GenericRepository<CourseSubject>(_context);
        RoleRepository = new GenericRepository<Role>(_context);
        DepartmentRepository = new GenericRepository<Department>(_context);
        LearnerTypeRepository = new GenericRepository<LearnerType>(_context);
        CourseRepository = new GenericRepository<Course>(_context);
        EvidenceTypeRepository = new GenericRepository<EvidenceType>(_context);
        SessionRepository = new GenericRepository<Session>(_context);
        SubjectResultRepository = new GenericRepository<SubjectResult>(_context);
        AttendanceRecordRepository = new GenericRepository<AttendanceRecord>(_context);
        AssessmentRepository = new GenericRepository<Assessment>(_context);
        AssessmentResultRepository = new GenericRepository<AssessmentResult>(_context);
        PracticalChecklistRepository = new GenericRepository<PracticalChecklist>(_context);
        PracticalChecklistResultRepository = new GenericRepository<PracticalChecklistResult>(_context);
        SubjectSignoffRepository = new GenericRepository<SubjectSignoff>(_context);
        RetakeHistoryRepository = new GenericRepository<RetakeHistory>(_context);
        EvidenceFileRepository = new GenericRepository<EvidenceFile>(_context);
        CompletionRequirementRepository = new GenericRepository<CompletionRequirement>(_context);
        ApprovalRequestRepository = new GenericRepository<ApprovalRequest>(_context);
        ApprovalHistoryRepository = new GenericRepository<ApprovalHistory>(_context);
        AuditLogRepository = new AuditLogRepository(_context);
        ExportJobRepository = new GenericRepository<ExportJob>(_context);
        DashboardSnapshotRepository = new GenericRepository<DashboardSnapshot>(_context);
    }

    public IGenericRepository<User> UserRepository { get; }
    public IGenericRepository<Learner> LearnerRepository { get; }
    public IGenericRepository<CourseEnrollment> CourseEnrollmentRepository { get; }
    public IGenericRepository<ETRCourseRecord> ETRCourseRecordRepository { get; }
    public IGenericRepository<Class> ClassRepository { get; }
    public IGenericRepository<Subject> SubjectRepository { get; }
    public IGenericRepository<CourseSubject> CourseSubjectRepository { get; }
    public IGenericRepository<Role> RoleRepository { get; }
    public IGenericRepository<Department> DepartmentRepository { get; }
    public IGenericRepository<LearnerType> LearnerTypeRepository { get; }
    public IGenericRepository<Course> CourseRepository { get; }
    public IGenericRepository<EvidenceType> EvidenceTypeRepository { get; }
    public IGenericRepository<Session> SessionRepository { get; }
    public IGenericRepository<SubjectResult> SubjectResultRepository { get; }
    public IGenericRepository<AttendanceRecord> AttendanceRecordRepository { get; }
    public IGenericRepository<Assessment> AssessmentRepository { get; }
    public IGenericRepository<AssessmentResult> AssessmentResultRepository { get; }
    public IGenericRepository<PracticalChecklist> PracticalChecklistRepository { get; }
    public IGenericRepository<PracticalChecklistResult> PracticalChecklistResultRepository { get; }
    public IGenericRepository<SubjectSignoff> SubjectSignoffRepository { get; }
    public IGenericRepository<RetakeHistory> RetakeHistoryRepository { get; }
    public IGenericRepository<EvidenceFile> EvidenceFileRepository { get; }
    public IGenericRepository<CompletionRequirement> CompletionRequirementRepository { get; }
    public IGenericRepository<ApprovalRequest> ApprovalRequestRepository { get; }
    public IGenericRepository<ApprovalHistory> ApprovalHistoryRepository { get; }
    public IAuditLogRepository AuditLogRepository { get; }
    public IGenericRepository<ExportJob> ExportJobRepository { get; }
    public IGenericRepository<DashboardSnapshot> DashboardSnapshotRepository { get; }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            if (_transaction != null)
                await _transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            if (_transaction != null)
                await _transaction.DisposeAsync();
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
        }
    }

    public async Task<int> SaveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<T> ExecuteInStrategyAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(operation, cancellationToken);
    }
}
