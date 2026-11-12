using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using ProjectDefense.Core.Common;
using ProjectDefense.Core.Dtos;
using ProjectDefense.Web.Data;
using ProjectDefense.Web.Services;

namespace ProjectDefense.Web.Pages.Lecturer.Reservations;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IReservationService _reservationService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public IndexModel(ApplicationDbContext context, IReservationService reservationService, IStringLocalizer<SharedResource> localizer)
    {
        _context = context;
        _reservationService = reservationService;
        _localizer = localizer;
    }

    [BindProperty(SupportsGet = true)]
    public int? RoomId { get; set; }

    [BindProperty(SupportsGet = true)]
    public SlotStatus? Status { get; set; }

    public SelectList RoomList { get; private set; } = default!;

    public SelectList StatusList { get; private set; } = default!;

    public IReadOnlyList<ReservationDto> Rows { get; private set; } = [];

    public async Task OnGetAsync()
    {
        RoomList = await RoomOptions.BuildAsync(_context, RoomId);
        StatusList = new SelectList(
            Enum.GetValues<SlotStatus>().Select(s => new { Value = s.ToString(), Text = _localizer["Status" + s].Value }),
            "Value",
            "Text",
            Status?.ToString());
        Rows = await _reservationService.GetReservationsAsync(new ReservationFilter(RoomId, Status));
    }

    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        var result = await _reservationService.CancelByLecturerAsync(id);
        Report(result, "ReservationCancelled");
        return RedirectToPage(new { RoomId, Status });
    }

    public async Task<IActionResult> OnPostUnblockAsync(int id)
    {
        var result = await _reservationService.UnblockAsync(id);
        Report(result, "SlotUnblocked");
        return RedirectToPage(new { RoomId, Status });
    }

    private void Report(OperationResult result, string successKey)
    {
        if (result.Succeeded)
        {
            TempData["Success"] = _localizer[successKey].Value;
        }
        else
        {
            TempData["Error"] = result.Message;
        }
    }
}
