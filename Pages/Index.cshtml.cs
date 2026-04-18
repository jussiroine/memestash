using System.Text.RegularExpressions;
using MemeStash.Endpoints;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MemeStash.Pages;

public class IndexModel : PageModel
{
    public void OnGet()
    {
    }

    public IActionResult OnPost(string slug)
    {
        slug = slug?.Trim().ToLowerInvariant() ?? string.Empty;

        if (!MemeEndpoints.IsValidSlug(slug))
        {
            ModelState.AddModelError("slug", "Invalid stash name. Use 3-30 characters: letters, numbers, and hyphens.");
            return Page();
        }

        return RedirectToPage("/Stash", new { slug });
    }
}
