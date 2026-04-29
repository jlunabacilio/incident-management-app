using IncidentReport.Application.Interfaces;

namespace IncidentReport.Infrastructure.Storage;

/// <summary>
/// Stores files on the local filesystem under /uploads/{incidentId}/{filename}.
/// Swap this for S3StorageService when deploying to cloud.
/// </summary>
public class LocalFileStorageService : IStorageService
{
    private readonly string _basePath;
    private readonly string _baseUrl;

    public LocalFileStorageService(string basePath, string baseUrl)
    {
        _basePath = basePath;
        _baseUrl = baseUrl.TrimEnd('/');
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> SaveAsync(string incidentId, string fileName, Stream content, string mimeType, CancellationToken ct = default)
    {
        var safeFileName = Path.GetFileName(fileName);
        var folder = Path.Combine(_basePath, incidentId);
        Directory.CreateDirectory(folder);

        var uniqueName = $"{Guid.NewGuid()}_{safeFileName}";
        var fullPath = Path.Combine(folder, uniqueName);

        await using var fs = File.Create(fullPath);
        await content.CopyToAsync(fs, ct);

        return $"{incidentId}/{uniqueName}";
    }

    public Task<string> GetUrlAsync(string storageKey, CancellationToken ct = default) =>
        Task.FromResult($"{_baseUrl}/uploads/{storageKey}");

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_basePath, storageKey.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }
}
