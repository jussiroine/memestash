using System.Text.RegularExpressions;
using MemeStash.Models;
using MemeStash.Services;
using Microsoft.Extensions.Options;

namespace MemeStash.Endpoints;

public static partial class MemeEndpoints
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/webp"
    };

    [GeneratedRegex(@"^[a-z0-9][a-z0-9\-]{1,28}[a-z0-9]$")]
    private static partial Regex SlugPattern();

    public static bool IsValidSlug(string slug) => SlugPattern().IsMatch(slug);

    public static void MapMemeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/memes");

        group.MapGet("/{slug}/{blobName}", async (string slug, string blobName, IMemeStorageService storage, CancellationToken ct) =>
        {
            slug = slug.ToLowerInvariant();
            if (!IsValidSlug(slug))
                return Results.BadRequest("Invalid stash name.");

            var result = await storage.GetAsync(slug, blobName, ct);
            if (result is null)
                return Results.NotFound();

            var (content, contentType) = result.Value;
            return Results.Stream(content, contentType, enableRangeProcessing: true);
        });

        group.MapDelete("/{slug}/{blobName}", async (string slug, string blobName, IMemeStorageService storage, CancellationToken ct) =>
        {
            slug = slug.ToLowerInvariant();
            if (!IsValidSlug(slug))
                return Results.BadRequest("Invalid stash name.");

            var deleted = await storage.DeleteAsync(slug, blobName, ct);
            return deleted ? Results.Ok() : Results.NotFound();
        });

        group.MapPut("/{slug}/{blobName}/pin", async (string slug, string blobName, IMemeStorageService storage, CancellationToken ct) =>
        {
            slug = slug.ToLowerInvariant();
            if (!IsValidSlug(slug))
                return Results.BadRequest("Invalid stash name.");

            var result = await storage.SetPinnedAsync(slug, blobName, true, ct);
            return result ? Results.Ok() : Results.NotFound();
        });

        group.MapDelete("/{slug}/{blobName}/pin", async (string slug, string blobName, IMemeStorageService storage, CancellationToken ct) =>
        {
            slug = slug.ToLowerInvariant();
            if (!IsValidSlug(slug))
                return Results.BadRequest("Invalid stash name.");

            var result = await storage.SetPinnedAsync(slug, blobName, false, ct);
            return result ? Results.Ok() : Results.NotFound();
        });

        group.MapPost("/{slug}", async (string slug, HttpRequest request, IMemeStorageService storage, IOptions<AzureStorageOptions> options, CancellationToken ct) =>
        {
            slug = slug.ToLowerInvariant();
            if (!IsValidSlug(slug))
                return Results.BadRequest("Invalid stash name.");

            if (!request.HasFormContentType)
                return Results.BadRequest("Expected multipart form data.");

            var form = await request.ReadFormAsync(ct);
            var file = form.Files.GetFile("file");
            if (file is null || file.Length == 0)
                return Results.BadRequest("No file uploaded.");

            if (file.Length > 5 * 1024 * 1024)
                return Results.BadRequest("File exceeds 5 MB limit.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                return Results.BadRequest($"File type '{extension}' is not allowed. Allowed: jpg, jpeg, png, gif, webp.");

            if (!AllowedContentTypes.Contains(file.ContentType))
                return Results.BadRequest("Invalid content type.");

            // Enforce per-slug limits
            var opts = options.Value;
            var stats = await storage.GetStatsAsync(slug, ct);

            if (stats.MemeCount >= opts.MaxMemesPerSlug)
                return Results.BadRequest($"Stash is full. Maximum {opts.MaxMemesPerSlug} memes per stash. Delete some to make room.");

            if (stats.TotalSizeBytes + file.Length > opts.MaxStoragePerSlugBytes)
                return Results.BadRequest($"Storage limit reached. Maximum {opts.MaxStoragePerSlugBytes / (1024 * 1024)} MB per stash. Delete some to free space.");

            using var stream = file.OpenReadStream();
            var meme = await storage.UploadAsync(slug, file.FileName, file.ContentType, stream, ct);

            return Results.Ok(new { meme.BlobName, meme.Url, meme.ContentType });
        }).DisableAntiforgery();
    }
}
