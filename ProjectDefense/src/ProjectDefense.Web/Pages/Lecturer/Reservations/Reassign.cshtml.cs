using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using ProjectDefense.Web.Data;
using ProjectDefense.Web.Services;

namespace ProjectDefense.Web.Pages.Lecturer.Reservations;

public class ReassignModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IReservationService _reservationService;
    private readonly ISlotLabelFormatter _labelFormatter;
    private readonly TimeProvider _timeProvider;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ReassignModel(
        ApplicationDbContext context,
        IReservationService reservationService,
        ISlotLabelFormatter labelFormatter,
        TimeProvider timeProvider,
        IStringLocalizer<SharedResource> localizer)
    {
        _context = context;
        _reservationService = reservationService;
        _labelFormatter = labelFormatter;
        _timeProvider = timeProvider;
        _localizer = localizer;
    }

    public string StudentName { get; private set; } = string.Empty;

    public string CurrentLabel { get; private set; } = string.Empty;

    public List<SelectListItem> Targets { get; private set; } = [];

    [BindProperty]
    public int TargetSlotId { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var slot = await _context.ReservationSlots
            .AsNoTracking()
            .Include(s => s.Student)
            .Include(s => s.LecturerAvailability!)
                .ThenInclude(a => a.Room)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (slot?.StudentId is null || slot.StartTime <= _timeProvider.GetLocalNow().DateTime)
        {
            TempData["Error"] = _localizer["ReservationNotFound"].Value;
            return RedirectToPage("Index");
        }

        StudentName = slot.Student?.FullName ?? slot.StudentId;
        CurrentLabel = _labelFormatter.Format(slot);
        Targets = (await _reservationService.GetAvailableSlotsAsync())
            .Select(s => new SelectListItem(_labelFormatter.Format(s), s.Id.ToString()))
            .ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var result = await _reservationService.ReassignAsync(id, TargetSlotId);
        if (result.Succeeded)
        {
            TempData["Success"] = _localizer["ReservationReassigned"].Value;
            return RedirectToPage("Index");
        }

        TempData["Error"] = result.Message;
        return RedirectToPage(new { id });
    }
}
