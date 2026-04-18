using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using MemeStash.Models;
using Microsoft.Extensions.Options;

namespace MemeStash.Services;

public class BlobStorageService : IMemeStorageService
{
    private readonly BlobContainerClient _container;

    public BlobStorageService(IOptions<AzureStorageOptions> options)
    {
        var opts = options.Value;
        var serviceClient = new BlobServiceClient(opts.ConnectionString);
        _container = serviceClient.GetBlobContainerClient(opts.ContainerName);
    }

    public async Task InitializeAsync()
    {
        await _container.CreateIfNotExistsAsync();
    }

    public async Task<MemeItem> UploadAsync(string slug, string fileName, string contentType, Stream content, CancellationToken ct = default)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var blobName = $"{Guid.NewGuid()}{extension}";
        var blobPath = $"{slug}/{blobName}";

        var blob = _container.GetBlobClient(blobPath);

        var headers = new BlobHttpHeaders { ContentType = contentType };
        var metadata = new Dictionary<string, string>
        {
            ["OriginalFileName"] = fileName,
            ["UploadedAt"] = DateTimeOffset.UtcNow.ToString("O")
        };

        await blob.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = headers,
            Metadata = metadata
        }, ct);

        return new MemeItem
        {
            BlobName = blobName,
            Slug = slug,
            ContentType = contentType,
            OriginalFileName = fileName,
            UploadedAt = DateTimeOffset.UtcNow,
            SizeBytes = content.Length
        };
    }

    public async Task<IReadOnlyList<MemeItem>> ListAsync(string slug, CancellationToken ct = default)
    {
        var items = new List<MemeItem>();
        var prefix = $"{slug}/";

        await foreach (var blob in _container.GetBlobsAsync(BlobTraits.Metadata, BlobStates.None, prefix, ct))
        {
            var blobName = blob.Name[prefix.Length..];
            var uploadedAt = blob.Metadata?.TryGetValue("UploadedAt", out var ts) == true
                ? DateTimeOffset.Parse(ts)
                : blob.Properties.CreatedOn ?? DateTimeOffset.MinValue;

            items.Add(new MemeItem
            {
                BlobName = blobName,
                Slug = slug,
                ContentType = blob.Properties.ContentType ?? "application/octet-stream",
                OriginalFileName = blob.Metadata?.TryGetValue("OriginalFileName", out var fn) == true ? fn : null,
                UploadedAt = uploadedAt,
                SizeBytes = blob.Properties.ContentLength ?? 0,
                IsPinned = blob.Metadata?.TryGetValue("Pinned", out var pinned) == true && pinned == "true"
            });
        }

        return items.OrderByDescending(m => m.UploadedAt).ToList();
    }

    public async Task<SlugStats> GetStatsAsync(string slug, CancellationToken ct = default)
    {
        var prefix = $"{slug}/";
        int count = 0;
        long totalSize = 0;

        await foreach (var blob in _container.GetBlobsAsync(BlobTraits.None, BlobStates.None, prefix, ct))
        {
            count++;
            totalSize += blob.Properties.ContentLength ?? 0;
        }

        return new SlugStats { MemeCount = count, TotalSizeBytes = totalSize };
    }

    public async Task<(Stream Content, string ContentType)?> GetAsync(string slug, string blobName, CancellationToken ct = default)
    {
        var blobPath = $"{slug}/{blobName}";
        var blob = _container.GetBlobClient(blobPath);

        if (!await blob.ExistsAsync(ct))
            return null;

        var response = await blob.DownloadStreamingAsync(cancellationToken: ct);
        var contentType = response.Value.Details.ContentType ?? "application/octet-stream";
        return (response.Value.Content, contentType);
    }

    public async Task<bool> DeleteAsync(string slug, string blobName, CancellationToken ct = default)
    {
        var blobPath = $"{slug}/{blobName}";
        var blob = _container.GetBlobClient(blobPath);
        var response = await blob.DeleteIfExistsAsync(cancellationToken: ct);
        return response.Value;
    }

    public async Task<bool> SetPinnedAsync(string slug, string blobName, bool pinned, CancellationToken ct = default)
    {
        var blobPath = $"{slug}/{blobName}";
        var blob = _container.GetBlobClient(blobPath);

        if (!await blob.ExistsAsync(ct))
            return false;

        var properties = await blob.GetPropertiesAsync(cancellationToken: ct);
        var metadata = properties.Value.Metadata;

        if (pinned)
            metadata["Pinned"] = "true";
        else
            metadata.Remove("Pinned");

        await blob.SetMetadataAsync(metadata, cancellationToken: ct);
        return true;
    }

    public async Task<IReadOnlyList<SlugSummary>> ListSlugsAsync(CancellationToken ct = default)
    {
        var slugs = new Dictionary<string, SlugSummary>();

        await foreach (var blob in _container.GetBlobsAsync(BlobTraits.None, BlobStates.None, string.Empty, ct))
        {
            var slashIndex = blob.Name.IndexOf('/');
            if (slashIndex < 0) continue;

            var slug = blob.Name[..slashIndex];
            var size = blob.Properties.ContentLength ?? 0;
            var created = blob.Properties.CreatedOn ?? DateTimeOffset.MinValue;

            if (slugs.TryGetValue(slug, out var existing))
            {
                slugs[slug] = existing with
                {
                    MemeCount = existing.MemeCount + 1,
                    TotalSizeBytes = existing.TotalSizeBytes + size,
                    LastUploadedAt = created > existing.LastUploadedAt ? created : existing.LastUploadedAt
                };
            }
            else
            {
                slugs[slug] = new SlugSummary
                {
                    Slug = slug,
                    MemeCount = 1,
                    TotalSizeBytes = size,
                    LastUploadedAt = created
                };
            }
        }

        return slugs.Values.OrderByDescending(s => s.LastUploadedAt).ToList();
    }
}
