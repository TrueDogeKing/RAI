using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using ProjectDefense.Core.Entities;
using ProjectDefense.Web.Data;
using ProjectDefense.Web.Services;

namespace ProjectDefense.Web.Pages.Lecturer.Availability;

public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IAvailabilityService _availabilityService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TimeProvider _timeProvider;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CreateModel(
        ApplicationDbContext context,
        IAvailabilityService availabilityService,
        UserManager<ApplicationUser> userManager,
        TimeProvider timeProvider,
        IStringLocalizer<SharedResource> localizer)
    {
        _context = context;
        _availabilityService = availabilityService;
        _userManager = userManager;
        _timeProvider = timeProvider;
        _localizer = localizer;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public SelectList RoomList { get; private set; } = default!;

    public async Task OnGetAsync()
    {
        var tomorrow = DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime).AddDays(1);
        Input = new InputModel
        {
            StartDate = tomorrow,
            EndDate = tomorrow,
            DailyStartTime = new TimeOnly(8, 0),
            DailyEndTime = new TimeOnly(10, 0),
            SlotDurationMinutes = 15
        };
        RoomList = await RoomOptions.BuildAsync(_context);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var request = Input.ToRequest();
        var today = DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);
        foreach (var key in AvailabilityValidator.Validate(request, today))
        {
            var field = FieldFor(key);
            if (ModelState.GetFieldValidationState(field) != Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Invalid)
            {
                ModelState.AddModelError(field, _localizer[key, AvailabilityValidator.MinSlotDuration, AvailabilityValidator.MaxSlotDuration]);
            }
        }

        if (!ModelState.IsValid)
        {
            RoomList = await RoomOptions.BuildAsync(_context, Input.RoomId);
            return Page();
        }

        var result = await _availabilityService.CreateAsync(_userManager.GetUserId(User)!, request);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message!);
            RoomList = await RoomOptions.BuildAsync(_context, Input.RoomId);
            return Page();
        }

        TempData["Success"] = _localizer["SlotsGenerated", result.Value!.Slots.Count].Value;
        return RedirectToPage("Index");
    }

    private static string FieldFor(string key) => key switch
    {
        "SlotDurationOutOfRange" => "Input.SlotDurationMinutes",
        "EndDateBeforeStartDate" => "Input.EndDate",
        "StartDateInPast" => "Input.StartDate",
        "DailyRangeTooShort" => "Input.DailyEndTime",
        _ => string.Empty
    };

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Required")]
        [Display(Name = "Room")]
        public int? RoomId { get; set; }

        [Required(ErrorMessage = "Required")]
        [Display(Name = "StartDate")]
        public DateOnly StartDate { get; set; }

        [Required(ErrorMessage = "Required")]
        [Display(Name = "EndDate")]
        public DateOnly EndDate { get; set; }

        [Required(ErrorMessage = "Required")]
        [Display(Name = "DailyStartTime")]
        public TimeOnly DailyStartTime { get; set; }

        [Required(ErrorMessage = "Required")]
        [Display(Name = "DailyEndTime")]
        public TimeOnly DailyEndTime { get; set; }

        [Range(AvailabilityValidator.MinSlotDuration, AvailabilityValidator.MaxSlotDuration, ErrorMessage = "Range")]
        [Display(Name = "SlotDurationMinutes")]
        public int SlotDurationMinutes { get; set; } = 15;

        public AvailabilityRequest ToRequest() =>
            new(RoomId ?? 0, StartDate, EndDate, DailyStartTime, DailyEndTime, SlotDurationMinutes);
    }
}
