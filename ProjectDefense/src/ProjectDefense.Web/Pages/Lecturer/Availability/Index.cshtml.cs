using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using ProjectDefense.Web.Services;

namespace ProjectDefense.Web.Pages.Lecturer.Availability;

public class IndexModel : PageModel
{
    private readonly IAvailabilityService _availabilityService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public IndexModel(IAvailabilityService availabilityService, IStringLocalizer<SharedResource> localizer)
    {
        _availabilityService = availabilityService;
        _localizer = localizer;
    }

    public IReadOnlyList<AvailabilitySummary> Items { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Items = await _availabilityService.GetAllAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var result = await _availabilityService.DeleteAsync(id);
        if (result.Succeeded)
        {
            TempData["Success"] = _localizer["AvailabilityDeleted"].Value;
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToPage();
    }
}
