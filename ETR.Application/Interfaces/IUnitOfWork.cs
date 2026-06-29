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
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    Task<int> SaveAsync(CancellationToken cancellationToken = default);
    Task<T> ExecuteInStrategyAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);
}
