using ETR.Application.Interfaces;
using ETR.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ETR.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();
        services.AddScoped<IEtrService, EtrService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IAssessmentService, AssessmentService>();
        services.AddScoped<IApprovalService, ApprovalService>();
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<IClassService, ClassService>();
        services.AddScoped<ISubjectService, SubjectService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<IEvidenceService, EvidenceService>();

        return services;
    }
}
