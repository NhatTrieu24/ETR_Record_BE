using ETR.Domain.Entities;

namespace ETR.Application.Interfaces;

public interface IETRCourseRecordRepository : IGenericRepository<ETRCourseRecord>
{
    Task<ETRCourseRecord?> GetWithSubjectResultsAsync(int id, CancellationToken cancellationToken = default);
}
