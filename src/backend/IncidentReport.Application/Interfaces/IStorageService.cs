namespace IncidentReport.Application.Interfaces;

public interface IStorageService
{
    /// <summary>Saves a file and returns the storage key.</summary>
    Task<string> SaveAsync(string incidentId, string fileName, Stream content, string mimeType, CancellationToken ct = default);

    /// <summary>Returns a URL to access the stored file.</summary>
    Task<string> GetUrlAsync(string storageKey, CancellationToken ct = default);

    /// <summary>Deletes a stored file by its storage key.</summary>
    Task DeleteAsync(string storageKey, CancellationToken ct = default);
}
