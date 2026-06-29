using ETR.Application.Interfaces;
using ETR.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ETR.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ILearnerService, LearnerService>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();
        services.AddScoped<IEtrService, EtrService>();
        services.AddScoped<ChecklistProgressInitializer>();

        return services;
    }
}
