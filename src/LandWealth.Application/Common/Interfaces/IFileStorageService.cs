namespace LandWealth.Application.Common.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveAsync(Stream content, CancellationToken cancellationToken = default);
    Stream OpenRead(string storageKey);
}
