using ETR.Application.Interfaces;
using ETR.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ETR.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // IHttpClientFactory — used by ExportService to fetch evidence file bytes from their
        // Cloudinary URL when assembling a Training Package ZIP (see ExportService.cs).
        services.AddHttpClient();

        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();
        services.AddScoped<IEtrService, EtrService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IAssessmentService, AssessmentService>();
        services.AddScoped<IAssessmentResultService, AssessmentResultService>();
        services.AddScoped<IApprovalService, ApprovalService>();
        services.AddScoped<IAmendmentService, AmendmentService>();
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<IClassService, ClassService>();
        services.AddScoped<ISubjectService, SubjectService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<IEvidenceService, EvidenceService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IEvidenceTypeService, EvidenceTypeService>();
        services.AddScoped<IPracticalChecklistService, PracticalChecklistService>();
        services.AddScoped<IPracticalChecklistResultService, PracticalChecklistResultService>();
        services.AddScoped<ICompletionRequirementService, CompletionRequirementService>();
        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IImportService, ImportService>();
        services.AddScoped<ICertificateExpiryNotificationService, CertificateExpiryNotificationService>();

        return services;
    }
}
