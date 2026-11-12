using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using ProjectDefense.Core.Entities;
using ProjectDefense.Web.Services;

namespace ProjectDefense.Web.Pages.Lecturer.Students;

public class IndexModel : PageModel
{
    private readonly IBanService _banService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public IndexModel(IBanService banService, UserManager<ApplicationUser> userManager, IStringLocalizer<SharedResource> localizer)
    {
        _banService = banService;
        _userManager = userManager;
        _localizer = localizer;
    }

    public IReadOnlyList<StudentSummary> Students { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Students = await _banService.GetStudentsAsync();
    }

    public async Task<IActionResult> OnPostBanAsync(string id, string? reason, DateOnly? bannedUntil)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = _localizer["BanReasonRequired"].Value;
            return RedirectToPage();
        }

        var until = bannedUntil?.ToDateTime(new TimeOnly(23, 59));
        var result = await _banService.BanAsync(id, _userManager.GetUserId(User)!, reason, until);
        if (result.Succeeded)
        {
            TempData["Success"] = _localizer["StudentBannedInfo"].Value;
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUnbanAsync(string id)
    {
        var result = await _banService.UnbanAsync(id);
        if (result.Succeeded)
        {
            TempData["Success"] = _localizer["StudentUnbanned"].Value;
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToPage();
    }
}
