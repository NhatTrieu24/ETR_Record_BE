using ETR.Application.DTOs;

namespace ETR.Application.Interfaces;

public interface ILogbookService
{
    Task<LogbookSummaryResponse> GetStudentLogbookSummaryAsync(int accountId, int currentAccountId, string roleName, CancellationToken cancellationToken = default);
}
