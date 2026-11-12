using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjectDefense.Core.Common;
using ProjectDefense.Core.Entities;
using ProjectDefense.Web.Services;

namespace ProjectDefense.Web.Pages;

public class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IReservationService _reservationService;
    private readonly ISlotLabelFormatter _labelFormatter;

    public IndexModel(UserManager<ApplicationUser> userManager, IReservationService reservationService, ISlotLabelFormatter labelFormatter)
    {
        _userManager = userManager;
        _reservationService = reservationService;
        _labelFormatter = labelFormatter;
    }

    public string FullName { get; private set; } = string.Empty;

    public string? ActiveReservationLabel { get; private set; }

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        FullName = user?.FullName ?? User.Identity?.Name ?? string.Empty;

        if (user is not null && User.IsInRole(Roles.Student))
        {
            var active = await _reservationService.GetActiveReservationAsync(user.Id);
            ActiveReservationLabel = active is null ? null : _labelFormatter.Format(active);
        }
    }
}
