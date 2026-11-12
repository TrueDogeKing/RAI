using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using ProjectDefense.Web.Data;
using ProjectDefense.Web.Services;

namespace ProjectDefense.Web.Pages.Lecturer.Block;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IReservationService _reservationService;
    private readonly TimeProvider _timeProvider;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public IndexModel(
        ApplicationDbContext context,
        IReservationService reservationService,
        TimeProvider timeProvider,
        IStringLocalizer<SharedResource> localizer)
    {
        _context = context;
        _reservationService = reservationService;
        _timeProvider = timeProvider;
        _localizer = localizer;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public SelectList RoomList { get; private set; } = default!;

    public async Task OnGetAsync()
    {
        var start = _timeProvider.GetLocalNow().DateTime.Date.AddDays(1).AddHours(8);
        Input = new InputModel { From = start, To = start.AddHours(2) };
        RoomList = await RoomOptions.BuildAsync(_context);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Input.To <= Input.From)
        {
            ModelState.AddModelError("Input.To", _localizer["BlockRangeInvalid"]);
        }

        if (!ModelState.IsValid)
        {
            RoomList = await RoomOptions.BuildAsync(_context, Input.RoomId);
            return Page();
        }

        var result = await _reservationService.BlockAsync(Input.RoomId!.Value, Input.From, Input.To);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message!);
            RoomList = await RoomOptions.BuildAsync(_context, Input.RoomId);
            return Page();
        }

        TempData["Success"] = _localizer["PeriodBlocked", result.Value!.BlockedSlots, result.Value.ReleasedReservations].Value;
        return RedirectToPage("/Lecturer/Reservations/Index", new { RoomId = Input.RoomId, Status = "Blocked" });
    }

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Required")]
        [Display(Name = "Room")]
        public int? RoomId { get; set; }

        [Required(ErrorMessage = "Required")]
        [Display(Name = "From")]
        public DateTime From { get; set; }

        [Required(ErrorMessage = "Required")]
        [Display(Name = "To")]
        public DateTime To { get; set; }
    }
}
