using MemeStash.Endpoints;
using MemeStash.Models;
using MemeStash.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace MemeStash.Pages;

public class StashModel : PageModel
{
    private readonly IMemeStorageService _storage;
    private readonly AzureStorageOptions _options;

    public StashModel(IMemeStorageService storage, IOptions<AzureStorageOptions> options)
    {
        _storage = storage;
        _options = options.Value;
    }

    public string Slug { get; set; } = string.Empty;
    public IReadOnlyList<MemeItem> Memes { get; set; } = [];
    public int MaxMemes => _options.MaxMemesPerSlug;
    public long MaxStorageBytes => _options.MaxStoragePerSlugBytes;
    public SlugStats Stats { get; set; } = new();
    public string Sort { get; set; } = "newest";

    public async Task<IActionResult> OnGetAsync(string slug, string? sort, CancellationToken ct)
    {
        slug = slug?.Trim().ToLowerInvariant() ?? string.Empty;

        if (!MemeEndpoints.IsValidSlug(slug))
            return RedirectToPage("/Index");

        Slug = slug;
        Sort = sort ?? "newest";
        var allMemes = await _storage.ListAsync(slug, ct);
        Stats = await _storage.GetStatsAsync(slug, ct);

        var pinned = allMemes.Where(m => m.IsPinned);
        var unpinned = allMemes.Where(m => !m.IsPinned);

        unpinned = Sort switch
        {
            "oldest" => unpinned.OrderBy(m => m.UploadedAt),
            "largest" => unpinned.OrderByDescending(m => m.SizeBytes),
            "smallest" => unpinned.OrderBy(m => m.SizeBytes),
            _ => unpinned.OrderByDescending(m => m.UploadedAt), // "newest"
        };

        pinned = Sort switch
        {
            "oldest" => pinned.OrderBy(m => m.UploadedAt),
            "largest" => pinned.OrderByDescending(m => m.SizeBytes),
            "smallest" => pinned.OrderBy(m => m.SizeBytes),
            _ => pinned.OrderByDescending(m => m.UploadedAt),
        };

        Memes = pinned.Concat(unpinned).ToList();
        return Page();
    }
}
