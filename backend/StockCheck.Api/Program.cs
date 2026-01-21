using Microsoft.AspNetCore.Authentication.Cookies;
using StockCheck.Api.Repositories;
using StockCheck.Api.Infrastructure;
using StockCheck.Api.Services;
using StockCheck.Api.BackgroundServices;

var builder = WebApplication.CreateBuilder(args);

// ===============================
// Services
// ===============================
builder.Services.AddControllers();

// ===============================
// CORS（Vite フロントエンド）
// ===============================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
             .AllowCredentials(); // ★ これが必須
    });
});

// ===============================
// 🔐 Authentication / Authorization
// ===============================
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/api/auth/login";
        options.AccessDeniedPath = "/api/auth/denied";
    });

builder.Services.AddAuthorization();

// ===============================
// Infrastructure
// ===============================
builder.Services.AddScoped<DbConnectionFactory>();
builder.Services.AddSingleton<PythonProcessRunner>();
// Import進捗通知用（SSE / 管理画面）
builder.Services.AddSingleton<ImportProgressChannel>();

// ===============================
// Repositories
// ===============================
builder.Services.AddScoped<SymbolRepository>();
builder.Services.AddScoped<WatchlistRepository>();
builder.Services.AddScoped<PriceDailyRepository>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<EpsPriceRepository>();
builder.Services.AddScoped<DrawdownRepository>();
builder.Services.AddScoped<PriceImportRepository>();
builder.Services.AddScoped<EpsImportRepository>();

// ===============================
// Services
// ===============================
builder.Services.AddScoped<AnalysisService>();
builder.Services.AddScoped<DrawdownService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<PasswordHasher>();
builder.Services.AddScoped<ImportService>();
builder.Services.AddScoped<PriceImportService>();
builder.Services.AddScoped<EpsImportService>();

// Background
builder.Services.AddHostedService<ImportBackgroundService>();

builder.Services.AddHttpContextAccessor();

// ===============================
// Swagger
// ===============================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ===============================
// Middleware
// ===============================
app.UseSwagger();
app.UseSwaggerUI();

// 🔥 この順番が超重要
app.UseRouting();
app.UseCors("AllowFrontend");

app.UseAuthentication();   // ★ 追加
app.UseAuthorization();

app.MapControllers();

app.Run();
