using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StockCheck.Api.Services;

namespace StockCheck.Api.BackgroundServices;

/// <summary>
/// 毎日決まった時刻（02:00）に ImportService を実行するバックグラウンドサービス
/// </summary>
public sealed class ImportBackgroundService : BackgroundService
{
    private static readonly TimeSpan EXECUTE_TIME = new(2, 0, 0); // 02:00 固定

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ImportBackgroundService> _logger;

    /// <summary>
    /// DIスコープ生成用Factoryとログ出力用Loggerを受け取る
    /// </summary>
    public ImportBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ImportBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// アプリ起動中、毎日02:00にImport処理を実行し続ける
    /// </summary>
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation("ImportBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 次に実行すべき02:00まで待機する
                await DelayUntilNextExecuteTimeAsync(stoppingToken);

                using var scope = _scopeFactory.CreateScope();

                var importService =
                    scope.ServiceProvider.GetRequiredService<ImportService>();

                // 全銘柄のImportを実行する
                _logger.LogInformation("Daily import started (02:00)");

                await importService.ImportAllForNightBatchAsync(stoppingToken);

                _logger.LogInformation("Daily import finished (02:00)");
            }
            catch (OperationCanceledException)
            {
                // アプリ終了時は正常終了扱い
                _logger.LogInformation("ImportBackgroundService cancelled");
                break;
            }
            catch (Exception ex)
            {
                // 1回失敗しても翌日は再実行される
                _logger.LogError(ex, "ImportBackgroundService failed");
            }
        }
    }

    /// <summary>
    /// 次の実行時刻（02:00）まで非同期で待機する
    /// </summary>
    private static async Task DelayUntilNextExecuteTimeAsync(
        CancellationToken ct)
    {
        var now = DateTime.Now;

        // 今日の02:00を基準にする
        var todayExecuteTime = now.Date + EXECUTE_TIME;

        // 既に02:00を過ぎていたら、翌日の02:00を次回実行時刻にする
        var nextExecuteTime =
            now < todayExecuteTime
                ? todayExecuteTime
                : todayExecuteTime.AddDays(1);

        var delay = nextExecuteTime - now;

        if (delay > TimeSpan.Zero)
        {
            await Task.Delay(delay, ct);
        }
    }
}
