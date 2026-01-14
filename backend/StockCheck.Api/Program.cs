using StockCheck.Api.Repositories;
using StockCheck.Api.Infrastructure;
using StockCheck.Api.Services;
using StockCheck.Api.BackgroundServices;

var builder = WebApplication.CreateBuilder(args);

// ===============================
// Services
// ===============================
builder.Services.AddControllers();

// 🔥 CORS（Vite フロントエンド許可）
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod(); // ← OPTIONS も含まれる
    });
});

// Infrastructure
builder.Services.AddScoped<DbConnectionFactory>();

// Repositories
builder.Services.AddScoped<SymbolRepository>();
builder.Services.AddScoped<WatchlistRepository>();
builder.Services.AddScoped<PriceDailyRepository>();
builder.Services.AddScoped<EarningsRepository>();
builder.Services.AddScoped<EarningsRepository>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<EpsPriceRepository>();
builder.Services.AddScoped<ExternalDataImporter>();
builder.Services.AddScoped<DrawdownRepository>();


// Services
builder.Services.AddScoped<AnalysisService>();
builder.Services.AddScoped<DrawdownService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<PasswordHasher>();
builder.Services.AddScoped<ImportService>();
builder.Services.AddHostedService<ImportBackgroundService>();



// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ===============================
// Middleware
// ===============================
app.UseSwagger();
app.UseSwaggerUI();

// 🔥 この3行の順番が命
app.UseRouting();
app.UseCors("AllowFrontend");
app.UseAuthorization();

app.MapControllers();

app.Run();
