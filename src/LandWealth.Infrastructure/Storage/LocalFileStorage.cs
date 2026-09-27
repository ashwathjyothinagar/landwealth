using LandWealth.Application.Common.Interfaces;
using LandWealth.Domain.Exceptions;
using Microsoft.Extensions.Options;

namespace LandWealth.Infrastructure.Storage;

public sealed class StorageSettings
{
    public string DocumentRoot { get; set; } = "./storage/documents";
}

public sealed class LocalFileStorage(IOptions<StorageSettings> options) : IFileStorageService
{
    private readonly string _root = Path.GetFullPath(options.Value.DocumentRoot);

    public async Task<string> SaveAsync(Stream content, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_root);
        var key = Guid.NewGuid().ToString("D");
        var path = ResolvePath(key);
        await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(file, cancellationToken);
        return key;
    }

    public Stream OpenRead(string storageKey)
    {
        var path = ResolvePath(storageKey);
        if (!File.Exists(path))
            throw new DomainException("The stored file was not found.");
        return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    private string ResolvePath(string storageKey)
    {
        if (storageKey.Contains('/') || storageKey.Contains('\\') || storageKey.Contains("..", StringComparison.Ordinal))
            throw new DomainException("Invalid storage key.");
        if (!Guid.TryParseExact(storageKey, "D", out var id))
            throw new DomainException("Invalid storage key.");

        var root = Path.GetFullPath(_root);
        var full = Path.GetFullPath(Path.Combine(root, id.ToString("D")));
        var relative = Path.GetRelativePath(root, full);
        if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
            throw new DomainException("Invalid storage key.");
        return full;
    }
}
