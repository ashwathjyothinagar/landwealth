using LandWealth.Domain.Exceptions;

namespace LandWealth.Application.Common;

public static class FileSignatures
{
    public static bool Matches(string contentType, ReadOnlySpan<byte> header)
    {
        return contentType.ToLowerInvariant() switch
        {
            "application/pdf" => StartsWith(header, 0x25, 0x50, 0x44, 0x46),
            "image/png" => StartsWith(header, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A),
            "image/jpeg" or "image/jpg" => StartsWith(header, 0xFF, 0xD8, 0xFF),
            "image/webp" => header.Length >= 12
                && StartsWith(header, 0x52, 0x49, 0x46, 0x46)
                && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50,
            _ => false
        };
    }

    private static bool StartsWith(ReadOnlySpan<byte> header, params byte[] signature)
        => header.Length >= signature.Length && header[..signature.Length].SequenceEqual(signature);
}

public static class DocumentFiles
{
    public static string RequireSafeFileName(string originalFileName)
    {
        var trimmed = originalFileName.Trim();
        var fileName = Path.GetFileName(trimmed);
        if (string.IsNullOrWhiteSpace(fileName) || fileName != trimmed || trimmed.Contains('/') || trimmed.Contains('\\'))
            throw new DomainException("The file name must not include a path.");
        return fileName;
    }
}
