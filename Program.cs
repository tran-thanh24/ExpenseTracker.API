using Microsoft.EntityFrameworkCore;
using ExpenseTracker.API.Data;     // Để tìm thấy AppDbContext
using ExpenseTracker.API.Services; // Để tìm thấy AuthService

var builder = WebApplication.CreateBuilder(args);

// Cấu hình CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", b => b.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
});

// Đăng ký Database (Sửa lỗi UseSqlServer)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Đăng ký các Service
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ExpenseService>(); // Đăng ký luôn cái này cho sau này

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors("AllowAll");
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.Run();