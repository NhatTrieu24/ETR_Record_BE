using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using ETR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ETR.Infrastructure.Repositories;

public class ETRCourseRecordRepository : GenericRepository<ETRCourseRecord>, IETRCourseRecordRepository
{
    private readonly AppDbContext _context;

    public ETRCourseRecordRepository(AppDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<ETRCourseRecord?> GetWithSubjectResultsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.ETRCourseRecords
            .Include(etr => etr.SubjectResults)
            .FirstOrDefaultAsync(etr => etr.ETRCourseRecordId == id, cancellationToken);
    }
}
