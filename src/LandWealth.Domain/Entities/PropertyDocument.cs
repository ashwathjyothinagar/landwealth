using LandWealth.Domain.Common;
using LandWealth.Domain.Enums;
using LandWealth.Domain.Exceptions;

namespace LandWealth.Domain.Entities;

public class PropertyDocument : AuditableEntity<Guid>
{
    private PropertyDocument() { }

    public Guid PropertyId { get; private set; }
    public Property? Property { get; private set; }
    public Guid? ParcelId { get; private set; }
    public PropertyParcel? Parcel { get; private set; }
    public DocumentType DocumentType { get; private set; }
    public string? DocumentNumber { get; private set; }
    public DateOnly? IssueDate { get; private set; }
    public string OriginalFileName { get; private set; } = default!;
    public string ContentType { get; private set; } = default!;
    public long FileSize { get; private set; }
    /// <summary>
    /// Internal GUID-based storage key. NEVER returned to clients.
    /// Physical path is derived server-side from StorageRoot + StorageKey.
    /// </summary>
    public string StorageKey { get; private set; } = default!;
    public string? Notes { get; private set; }

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg", "image/jpg", "image/png", "image/webp"
    };

    private const long MaxFileSizeBytes = 25 * 1024 * 1024; // 25 MB

    public static PropertyDocument Create(
        Guid propertyId, Guid userId,
        DocumentType documentType,
        string originalFileName, string contentType, long fileSize,
        string storageKey, Guid createdBy,
        Guid? parcelId = null,
        string? documentNumber = null,
        DateOnly? issueDate = null,
        string? notes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originalFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);

        if (!AllowedContentTypes.Contains(contentType))
            throw new DomainException(
                $"File type '{contentType}' is not allowed. Only PDF and common image formats are permitted.");

        if (fileSize <= 0 || fileSize > MaxFileSizeBytes)
            throw new DomainException($"File size must be between 1 byte and {MaxFileSizeBytes / (1024 * 1024)} MB.");

        return new PropertyDocument
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            UserId = userId,
            ParcelId = parcelId,
            DocumentType = documentType,
            DocumentNumber = documentNumber?.Trim(),
            IssueDate = issueDate,
            OriginalFileName = originalFileName.Trim(),
            ContentType = contentType.Trim(),
            FileSize = fileSize,
            StorageKey = storageKey,
            Notes = notes?.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }
}
