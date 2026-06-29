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
    }

    public IGenericRepository<User> UserRepository { get; }
    public IGenericRepository<Learner> LearnerRepository { get; }
    public IGenericRepository<Enrollment> EnrollmentRepository { get; }
    public IGenericRepository<ETRRecord> ETRRecordRepository { get; }
    public IGenericRepository<TrainingClass> TrainingClassRepository { get; }
    public IGenericRepository<ETRChecklistTemplate> ETRChecklistTemplateRepository { get; }
    public IGenericRepository<ETRChecklistItem> ETRChecklistItemRepository { get; }
    public IGenericRepository<ETRChecklistProgress> ETRChecklistProgressRepository { get; }

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
