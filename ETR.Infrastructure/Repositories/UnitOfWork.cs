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
        EnrollmentRepository = new GenericRepository<Enrollment>(_context);
        ETRRecordRepository = new GenericRepository<ETRRecord>(_context);
        TrainingClassRepository = new GenericRepository<TrainingClass>(_context);
        ETRChecklistTemplateRepository = new GenericRepository<ETRChecklistTemplate>(_context);
        ETRChecklistItemRepository = new GenericRepository<ETRChecklistItem>(_context);
        ETRChecklistProgressRepository = new GenericRepository<ETRChecklistProgress>(_context);
        RoleRepository = new GenericRepository<Role>(_context);
        DepartmentRepository = new GenericRepository<Department>(_context);
        LearnerTypeRepository = new GenericRepository<LearnerType>(_context);
        CourseRepository = new GenericRepository<Course>(_context);
        EvidenceTypeRepository = new GenericRepository<EvidenceType>(_context);
        ClassInstructorRepository = new GenericRepository<ClassInstructor>(_context);
        CompletionRequirementRepository = new GenericRepository<CompletionRequirement>(_context);
        AttendanceSessionRepository = new GenericRepository<AttendanceSession>(_context);
        AttendanceRecordRepository = new GenericRepository<AttendanceRecord>(_context);
        AssessmentComponentRepository = new GenericRepository<AssessmentComponent>(_context);
        AssessmentResultRepository = new GenericRepository<AssessmentResult>(_context);
        EvidenceFileRepository = new GenericRepository<EvidenceFile>(_context);
        ApprovalRequestRepository = new GenericRepository<ApprovalRequest>(_context);
        ApprovalHistoryRepository = new GenericRepository<ApprovalHistory>(_context);
        AuditLogRepository = new AuditLogRepository(_context);
        ExportJobRepository = new GenericRepository<ExportJob>(_context);
        DashboardSnapshotRepository = new GenericRepository<DashboardSnapshot>(_context);
    }

    public IGenericRepository<User> UserRepository { get; }
    public IGenericRepository<Learner> LearnerRepository { get; }
    public IGenericRepository<Enrollment> EnrollmentRepository { get; }
    public IGenericRepository<ETRRecord> ETRRecordRepository { get; }
    public IGenericRepository<TrainingClass> TrainingClassRepository { get; }
    public IGenericRepository<ETRChecklistTemplate> ETRChecklistTemplateRepository { get; }
    public IGenericRepository<ETRChecklistItem> ETRChecklistItemRepository { get; }
    public IGenericRepository<ETRChecklistProgress> ETRChecklistProgressRepository { get; }
    public IGenericRepository<Role> RoleRepository { get; }
    public IGenericRepository<Department> DepartmentRepository { get; }
    public IGenericRepository<LearnerType> LearnerTypeRepository { get; }
    public IGenericRepository<Course> CourseRepository { get; }
    public IGenericRepository<EvidenceType> EvidenceTypeRepository { get; }
    public IGenericRepository<ClassInstructor> ClassInstructorRepository { get; }
    public IGenericRepository<CompletionRequirement> CompletionRequirementRepository { get; }
    public IGenericRepository<AttendanceSession> AttendanceSessionRepository { get; }
    public IGenericRepository<AttendanceRecord> AttendanceRecordRepository { get; }
    public IGenericRepository<AssessmentComponent> AssessmentComponentRepository { get; }
    public IGenericRepository<AssessmentResult> AssessmentResultRepository { get; }
    public IGenericRepository<EvidenceFile> EvidenceFileRepository { get; }
    public IGenericRepository<ApprovalRequest> ApprovalRequestRepository { get; }
    public IGenericRepository<ApprovalHistory> ApprovalHistoryRepository { get; }
    public IAuditLogRepository AuditLogRepository { get; }
    public IGenericRepository<ExportJob> ExportJobRepository { get; }
    public IGenericRepository<DashboardSnapshot> DashboardSnapshotRepository { get; }

    public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return BeginTransactionInternalAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            return;
        }

        await _transaction.CommitAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            return;
        }

        await _transaction.RollbackAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public Task<int> SaveAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<T> ExecuteInStrategyAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async (ct) =>
        {
            return await operation(ct);
        }, cancellationToken);
    }

    private async Task BeginTransactionInternalAsync(CancellationToken cancellationToken)
    {
        if (_transaction is not null)
        {
            return;
        }

        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }
}
