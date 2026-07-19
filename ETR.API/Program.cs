using System.Text;
using Microsoft.Extensions.Hosting;
using ETR.API.Services;
using ETR.Application;
using ETR.Application.Interfaces;
using ETR.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

try
{
    var builder = WebApplication.CreateBuilder(args);

    // 1. Thêm cấu hình Controllers và Swagger/Endpoints
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    // Cấu hình Swagger để hỗ trợ JWT Authentication
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo { Title = "Aircraft Training ETR API", Version = "v1" });
        
        c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
            Name = "Authorization",
            In = Microsoft.OpenApi.ParameterLocation.Header,
            Type = Microsoft.OpenApi.SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
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

    var app = builder.Build();

    // 3. Cấu hình HTTP request pipeline (Middleware)
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