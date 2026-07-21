using ETR.API.Middleware;
using ETR.API.Services;
using ETR.Application;
using ETR.Application.Interfaces;
using ETR.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using System.Threading.RateLimiting;

try
{
    var builder = WebApplication.CreateBuilder(args);

    // 1. Thêm cấu hình Controllers và Swagger/Endpoints
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // Cấu hình Swagger để hỗ trợ JWT Authentication
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Aircraft Training ETR API", Version = "v1" });

        // Tạo ô nhập token trên giao diện Swagger
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "Nhập token của bạn vào đây (KHÔNG CẦN gõ chữ 'Bearer ' ở trước).",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });

        c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecuritySchemeReference("Bearer", document, null),
                new List<string>()
            }
        });

        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }
    });

    // Cấu hình JWT Authentication
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var secretKey = jwtSettings.GetValue<string>("SecretKey") ?? throw new InvalidOperationException("JWT SecretKey is missing.");

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.GetValue<string>("Issuer"),
            ValidAudience = jwtSettings.GetValue<string>("Audience"),
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

    // 2. Đăng ký các dịch vụ thuộc Clean Architecture các tầng
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // Explicitly add Authorization (critical for [Authorize] attributes to map policies)
    builder.Services.AddAuthorization();

    // Global exception handler → ProblemDetails (no stack traces leaked to clients)
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // Rate limiting cho các endpoint auth nhạy cảm (chống brute-force)
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy("AuthPolicy", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
    });

    // CẤU HÌNH CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll",
            policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
    });

    var app = builder.Build();

    // 3. Cấu hình HTTP request pipeline (Middleware)
    app.UseExceptionHandler();

    if (app.Environment.IsDevelopment())
    {
        // Kích hoạt giao diện Swagger trực quan để test API
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Aircraft Training ETR API v1");
            c.RoutePrefix = string.Empty; // Mở ứng dụng phát vào thẳng Swagger luôn (không cần gõ /swagger)
        });
    }

    app.UseHttpsRedirection();

    // ĐĂNG KÝ MIDDLEWARE CORS
    app.UseCors("AllowAll");

    app.UseRateLimiter();

    // Middleware xác thực (Authentication) phải nằm trước phân quyền (Authorization)
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    Console.WriteLine("=== HỆ THỐNG ETR ĐANG KHỞI CHẠY THÀNH CÔNG ===");
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<ETR.Infrastructure.Data.AppDbContext>();
            await Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.MigrateAsync(context.Database);
            await ETR.Infrastructure.Data.DataSeeder.SeedAsync(context);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while migrating or seeding the database.");
        }
    }

    app.Run();
}
catch (HostAbortedException)
{
    // Ignore EF Core tooling abort
    throw;
}
catch (Exception ex)
{
    Console.WriteLine("==================================================");
    Console.WriteLine("❌ ỨNG DỤNG BỊ SẬP KHI KHỞI ĐỘNG! CHI TIẾT LỖI:");
    Console.WriteLine($"Thông điệp: {ex.Message}");
    Console.WriteLine($"Vị trí lỗi (StackTrace):\n{ex.StackTrace}");
    Console.WriteLine("==================================================");
    throw;
}