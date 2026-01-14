using System.Diagnostics;

namespace StockCheck.Api.Services;

/// <summary>
/// 外部データ取得（Python 呼び出し専用）
/// </summary>
public class ExternalDataImporter
{
    private const string PythonProjectRoot = @"C:\Power-law\python";
    private const string PythonExePath =
        @"C:\Power-law\python\.venv\Scripts\python.exe";

    // =====================================================
    // 株価（日次）
    // =====================================================
    public async Task ImportPriceAsync(string symbol, DateTime fromDate)
    {
        await RunPythonAsync(
            scriptRelativePath: @"src\scripts\import_price_daily.py",
            args: $"{symbol} {fromDate:yyyy-MM-dd}"
        );
    }

    // =====================================================
    // EPS（四半期）
    // =====================================================
    public async Task ImportEpsAsync(string symbol, int maxCount)
    {
        await RunPythonAsync(
            scriptRelativePath: @"src\scripts\import_eps_quarterly.py",
            args: $"{symbol} {maxCount}"
        );
    }

    // =====================================================
    // Python 実行共通処理
    // =====================================================
    private static async Task RunPythonAsync(string scriptRelativePath, string args)
    {
        var scriptFullPath = Path.Combine(PythonProjectRoot, scriptRelativePath);

        var psi = new ProcessStartInfo
        {
            FileName = PythonExePath,
            Arguments = $"\"{scriptFullPath}\" {args}",
            WorkingDirectory = PythonProjectRoot,

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

        var stdOut = await process.StandardOutput.ReadToEndAsync();
        var stdErr = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Python failed: {scriptRelativePath}\n{stdErr}");
        }
    }
}
