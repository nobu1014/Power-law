using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using StockCheck.Api.Services;

namespace StockCheck.Api.BackgroundServices;

public class ImportBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ImportBackgroundService> _logger;

    public ImportBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ImportBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 起動直後に即実行したいなら delay なし
        // await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var importService =
                    scope.ServiceProvider.GetRequiredService<ImportService>();

                _logger.LogInformation("Daily import started");

                await importService.ImportAllAsync(
                    maxPriceYears: 5,
                    maxEpsQuarters: 16,
                    ct: stoppingToken
                );

                _logger.LogInformation("Daily import finished");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Import background job failed");
            }

            // ★ 1日1回
            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }
}
