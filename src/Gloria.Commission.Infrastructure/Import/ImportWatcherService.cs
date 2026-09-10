using Gloria.Commission.Application.Services;
using Gloria.Commission.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gloria.Commission.Infrastructure.Import;

/// <summary>
/// Gecelik toplu aktarim. Kaynak sistemlerin biraktigi ekstreleri bulup
/// HTTP ile ayni <see cref="IImportService"/> uzerinden isler — aktarim mantigi tek yerde.
///
/// Dosya adinin on eki kaynagi belirler: pms_*, pos_*, erp_*.
/// Islenen dosya arsive, islenemeyen ayri bir klasore tasinir; ayni dosya
/// tekrar okunmaya calisilmaz.
/// </summary>
public sealed class ImportWatcherService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ImportWatcherOptions _options;
    private readonly ILogger<ImportWatcherService> _logger;

    public ImportWatcherService(
        IServiceScopeFactory scopeFactory,
        IOptions<ImportWatcherOptions> options,
        ILogger<ImportWatcherService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Zamanlanmis aktarim kapali (ImportWatcher:Enabled=false).");
            return;
        }

        _logger.LogInformation(
            "Zamanlanmis aktarim acik. Klasor: {InboxPath}, aralik: {Interval}",
            _options.InboxPath, _options.Interval);

        try
        {
            await Task.Delay(_options.InitialDelay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await RunOnceAsync(stoppingToken);
                await Task.Delay(_options.Interval, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Uygulama kapaniyor; normal cikis.
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            Directory.CreateDirectory(_options.InboxPath);
            Directory.CreateDirectory(_options.ProcessedPath);
            Directory.CreateDirectory(_options.FailedPath);

            var files = Directory.GetFiles(_options.InboxPath, "*.csv").OrderBy(f => f).ToList();
            if (files.Count == 0) return;

            _logger.LogInformation("{FileCount} dosya bulundu, aktarim basliyor.", files.Count);

            foreach (var file in files)
            {
                if (ct.IsCancellationRequested) break;
                await ProcessFileAsync(file, ct);
            }
        }
        catch (Exception ex)
        {
            // Tarama hatasi servisi durdurmaz; bir sonraki turda yeniden denenir.
            _logger.LogError(ex, "Zamanlanmis aktarim taramasi basarisiz.");
        }
    }

    private async Task ProcessFileAsync(string path, CancellationToken ct)
    {
        var fileName = Path.GetFileName(path);

        if (!TryResolveSource(fileName, out var source))
        {
            _logger.LogWarning(
                "{FileName} dosyasinin kaynagi belirlenemedi; beklenen on ekler: pms_, pos_, erp_.",
                fileName);

            Move(path, _options.FailedPath);
            return;
        }

        try
        {
            var content = await File.ReadAllTextAsync(path, ct);

            // BackgroundService singleton; scoped servisler icin kendi kapsamini acar.
            using var scope = _scopeFactory.CreateScope();
            var importService = scope.ServiceProvider.GetRequiredService<IImportService>();

            var summary = await importService.ImportAsync(source, fileName, content, ct);

            _logger.LogInformation(
                "{FileName} aktarildi. Yazilan: {ImportedRows}, mukerrer: {DuplicateRows}, hatali: {FailedRows}",
                fileName, summary.ImportedRows, summary.DuplicateRows, summary.FailedRows);

            Move(path, _options.ProcessedPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{FileName} aktarilamadi.", fileName);
            Move(path, _options.FailedPath);
        }
    }

    private static bool TryResolveSource(string fileName, out SourceSystem source)
    {
        var name = fileName.ToLowerInvariant();

        if (name.StartsWith("pms_", StringComparison.Ordinal)) { source = SourceSystem.Pms; return true; }
        if (name.StartsWith("pos_", StringComparison.Ordinal)) { source = SourceSystem.Pos; return true; }
        if (name.StartsWith("erp_", StringComparison.Ordinal)) { source = SourceSystem.Erp; return true; }

        source = default;
        return false;
    }

    /// <summary>Dosyayi hedefe tasir; ayni ada sahip dosya varsa zaman damgasi eklenir.</summary>
    private static void Move(string path, string targetDirectory)
    {
        var target = Path.Combine(targetDirectory, Path.GetFileName(path));

        if (File.Exists(target))
        {
            var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            target = Path.Combine(
                targetDirectory,
                $"{Path.GetFileNameWithoutExtension(path)}_{stamp}{Path.GetExtension(path)}");
        }

        File.Move(path, target);
    }
}
