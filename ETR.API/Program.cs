using System.Text;
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
    builder.Services.AddSwaggerGen();

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
    app.Run();
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