using MemeStash.Models;
using MemeStash.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MemeStash.Pages;

public class BrowseModel : PageModel
{
    private readonly IMemeStorageService _storage;

    public BrowseModel(IMemeStorageService storage)
    {
        _storage = storage;
    }

    public IReadOnlyList<SlugSummary> Stashes { get; set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        Stashes = await _storage.ListSlugsAsync(ct);
    }
}
