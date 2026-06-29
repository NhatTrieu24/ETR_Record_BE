using ETR.Application;
using ETR.Infrastructure;

try
{
    var builder = WebApplication.CreateBuilder(args);

    // 1. Thêm cấu hình Controllers và Swagger/Endpoints
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(); // Đảm bảo cấu hình tài liệu API mượt mà cho Controller

    // 2. Đăng ký các dịch vụ thuộc Clean Architecture các tầng
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

    app.UseAuthorization(); // Thêm middleware phân quyền mặc định

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