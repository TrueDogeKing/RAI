using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using ProjectDefense.Core.Common;
using ProjectDefense.Core.Entities;
using ProjectDefense.Web.Data;
using ProjectDefense.Web.Pages.Lecturer;
using ProjectDefense.Web.Services;

namespace ProjectDefense.Web.Pages.Student;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IReservationService _reservationService;
    private readonly IBanService _banService;
    private readonly ISlotLabelFormatter _labelFormatter;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public IndexModel(
        ApplicationDbContext context,
        IReservationService reservationService,
        IBanService banService,
        ISlotLabelFormatter labelFormatter,
        UserManager<ApplicationUser> userManager,
        IStringLocalizer<SharedResource> localizer)
    {
        _context = context;
        _reservationService = reservationService;
        _banService = banService;
        _labelFormatter = labelFormatter;
        _userManager = userManager;
        _localizer = localizer;
    }

    [BindProperty(SupportsGet = true)]
    public int? RoomId { get; set; }

    public ReservationSlot? Active { get; private set; }

    public string? ActiveLabel { get; private set; }

    public bool IsBanned { get; private set; }

    public SelectList RoomList { get; private set; } = default!;

    public IReadOnlyList<SlotView> Slots { get; private set; } = [];

    private string StudentId => _userManager.GetUserId(User)!;

    public async Task OnGetAsync()
    {
        IsBanned = await _banService.IsBannedAsync(StudentId);
        Active = await _reservationService.GetActiveReservationAsync(StudentId);
        ActiveLabel = Active is null ? null : _labelFormatter.Format(Active);
        RoomList = await RoomOptions.BuildAsync(_context, RoomId);
        Slots = (await _reservationService.GetAvailableSlotsAsync(RoomId))
            .Select(s => new SlotView(s.Id, _labelFormatter.Format(s), s.LecturerAvailability?.Lecturer?.FullName ?? string.Empty))
            .ToList();
    }

    public async Task<IActionResult> OnPostBookAsync(int id)
    {
        var result = await _reservationService.BookAsync(id, StudentId);
        return Report(result.Succeeded, result.Message, "ReservationCreated");
    }

    public async Task<IActionResult> OnPostRescheduleAsync(int id)
    {
        var result = await _reservationService.RescheduleAsync(StudentId, id);
        return Report(result.Succeeded, result.Message, "ReservationRescheduled");
    }

    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        var result = await _reservationService.CancelAsync(id, StudentId);
        return Report(result.Succeeded, result.Message, "ReservationCancelled");
    }

    private IActionResult Report(bool succeeded, string? message, string successKey)
    {
        if (succeeded)
        {
            TempData["Success"] = _localizer[successKey].Value;
        }
        else
        {
            TempData["Error"] = message;
        }

        return RedirectToPage(new { RoomId });
    }

    public sealed record SlotView(int Id, string Label, string LecturerName);
}
