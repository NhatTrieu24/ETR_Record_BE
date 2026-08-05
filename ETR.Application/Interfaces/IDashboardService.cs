using ETR.Application.DTOs;

namespace ETR.Application.Interfaces;

public interface IDashboardService
{
    Task<MyDashboardResponse> GetMyDashboardAsync(CancellationToken cancellationToken = default);
}
