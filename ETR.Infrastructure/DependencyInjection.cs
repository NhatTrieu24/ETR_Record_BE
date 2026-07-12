using ETR.Application.Interfaces;
using ETR.Infrastructure.Data;
using ETR.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ETR.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions =>
                {
                    // Giải pháp vàng: Kích hoạt khả năng phục hồi lỗi kết nối tạm thời cho SQL Server
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                }));

        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.Configure<ETR.Infrastructure.Identity.JwtOptions>(
            configuration.GetSection(ETR.Infrastructure.Identity.JwtOptions.SectionName));
        services.AddScoped<ITokenService, ETR.Infrastructure.Identity.TokenService>();

        return services;
    }
}