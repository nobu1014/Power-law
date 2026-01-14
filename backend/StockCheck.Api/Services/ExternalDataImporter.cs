using System.Diagnostics;

namespace StockCheck.Api.Services;

/// <summary>
/// 外部データ取得（Phase0）
///
/// 【責務】
/// ・株価 / EPS データを
///   Python スクリプト経由で取得し DB に登録する
/// </summary>
public class ExternalDataImporter
{
    // ★ Python プロジェクトのルート
    private const string PythonProjectRoot = @"C:\Power-law\python";

    // ★ venv の python.exe を明示
    private const string PythonExePath =
        @"C:\Power-law\python\.venv\Scripts\python.exe";

    /// <summary>
    /// 株価（日次）データを取得して DB に登録する
    /// </summary>
    public async Task ImportPriceAsync(string symbol)
    {
        await RunPythonAsync(
            scriptRelativePath: @"src\scripts\import_price_daily.py",
            symbol: symbol
        );
    }

    /// <summary>
    /// EPS（四半期）データを取得して DB に登録する
    /// </summary>
    public async Task ImportEpsAsync(string symbol)
    {
        await RunPythonAsync(
            scriptRelativePath: @"src\scripts\import_eps_quarterly.py",
            symbol: symbol
        );
    }

    /// <summary>
    /// Python スクリプト実行共通処理
    /// </summary>
    private static async Task RunPythonAsync(string scriptRelativePath, string symbol)
    {
        var scriptFullPath = Path.Combine(PythonProjectRoot, scriptRelativePath);

        var psi = new ProcessStartInfo
        {
            FileName = PythonExePath,
            Arguments = $"\"{scriptFullPath}\" {symbol}",
            WorkingDirectory = PythonProjectRoot,

            // ★ これが超重要
            Environment =
            {
                ["PYTHONPATH"] = Path.Combine(PythonProjectRoot, "src")
            },

            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start python process.");

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException(
                $"Python script failed: {scriptRelativePath}\n{error}");
        }
    }

}
