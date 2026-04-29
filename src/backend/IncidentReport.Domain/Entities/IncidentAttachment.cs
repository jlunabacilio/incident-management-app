namespace IncidentReport.Domain.Entities;

/// <summary>
/// Represents a photo attachment linked to a maintenance incident.
/// </summary>
public class IncidentAttachment
{
    private static readonly string[] AllowedMimeTypes = ["image/jpeg", "image/png", "image/webp"];
    private const long MaxSizeBytes = 10 * 1024 * 1024; // 10 MB

    public Guid Id { get; private set; }
    public Guid IncidentId { get; private set; }
    public string StorageKey { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string MimeType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public string UploadedBy { get; private set; } = string.Empty;

    // Required by EF Core
    private IncidentAttachment() { }

    public static IncidentAttachment Create(
        Guid incidentId,
        string storageKey,
        string fileName,
        string mimeType,
        long sizeBytes,
        string uploadedBy)
    {
        if (!AllowedMimeTypes.Contains(mimeType.ToLowerInvariant()))
            throw new ArgumentException($"File type '{mimeType}' is not allowed. Accepted: JPEG, PNG, WEBP.", nameof(mimeType));
        if (sizeBytes > MaxSizeBytes)
            throw new ArgumentException($"File size {sizeBytes} bytes exceeds the 10 MB limit.", nameof(sizeBytes));

        return new IncidentAttachment
        {
            Id = Guid.NewGuid(),
            IncidentId = incidentId,
            StorageKey = storageKey,
            FileName = fileName,
            MimeType = mimeType.ToLowerInvariant(),
            SizeBytes = sizeBytes,
            UploadedAt = DateTime.UtcNow,
            UploadedBy = uploadedBy
        };
    }
}
