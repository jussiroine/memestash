using MemeStash.Models;

namespace MemeStash.Services;

public interface IMemeStorageService
{
    Task<MemeItem> UploadAsync(string slug, string fileName, string contentType, Stream content, CancellationToken ct = default);
    Task<IReadOnlyList<MemeItem>> ListAsync(string slug, CancellationToken ct = default);
    Task<SlugStats> GetStatsAsync(string slug, CancellationToken ct = default);
    Task<(Stream Content, string ContentType)?> GetAsync(string slug, string blobName, CancellationToken ct = default);
    Task<bool> DeleteAsync(string slug, string blobName, CancellationToken ct = default);
    Task<bool> SetPinnedAsync(string slug, string blobName, bool pinned, CancellationToken ct = default);
    Task<IReadOnlyList<SlugSummary>> ListSlugsAsync(CancellationToken ct = default);
}
