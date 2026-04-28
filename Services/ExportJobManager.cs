using System.Collections.Concurrent;

namespace Sasc26.Services;

public class ExportJobInfo
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = "pending"; // pending, processing, complete, failed
    public string? FilePath { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ExportJobManager
{
    private readonly ConcurrentDictionary<string, ExportJobInfo> _jobs = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExportJobManager> _logger;

    public ExportJobManager(IServiceProvider serviceProvider, ILogger<ExportJobManager> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public string StartExport()
    {
        var jobId = Guid.NewGuid().ToString("N");
        var tempDir = Path.Combine(Path.GetTempPath(), "sasc26_exports");
        Directory.CreateDirectory(tempDir);
        var filePath = Path.Combine(tempDir, $"certificados_sasc26_{jobId}.zip");

        var job = new ExportJobInfo
        {
            JobId = jobId,
            Status = "processing",
            FilePath = filePath,
            CreatedAt = DateTime.UtcNow
        };
        _jobs[jobId] = job;

        // Fire-and-forget the background work
        _ = ProcessExportAsync(jobId);

        return jobId;
    }

    public ExportJobInfo? GetStatus(string jobId)
    {
        _jobs.TryGetValue(jobId, out var job);
        return job;
    }

    public byte[]? ConsumeFile(string jobId)
    {
        if (!_jobs.TryGetValue(jobId, out var job))
            return null;

        if (job.Status != "complete" || job.FilePath == null || !File.Exists(job.FilePath))
            return null;

        var bytes = File.ReadAllBytes(job.FilePath);

        // Clean up the temp file and remove the job
        try { File.Delete(job.FilePath); } catch { }
        _jobs.TryRemove(jobId, out _);

        return bytes;
    }

    private async Task ProcessExportAsync(string jobId)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var certService = scope.ServiceProvider.GetRequiredService<ICertificateService>();

            if (!_jobs.TryGetValue(jobId, out var job) || job.FilePath == null)
                return;

            await certService.ExportAllCertificatesToFileAsync(job.FilePath);

            if (_jobs.TryGetValue(jobId, out var updatedJob))
            {
                updatedJob.Status = "complete";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export job {JobId} failed", jobId);
            if (_jobs.TryGetValue(jobId, out var failedJob))
            {
                failedJob.Status = "failed";
                failedJob.ErrorMessage = ex.Message;
            }
        }
    }
}
