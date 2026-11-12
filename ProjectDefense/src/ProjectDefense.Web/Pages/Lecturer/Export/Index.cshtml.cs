using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using ProjectDefense.Core.Common;
using ProjectDefense.Web.Data;
using ProjectDefense.Web.Services;

namespace ProjectDefense.Web.Pages.Lecturer.Export;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IExportService _exportService;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public IndexModel(ApplicationDbContext context, IExportService exportService, IStringLocalizer<SharedResource> localizer)
    {
        _context = context;
        _exportService = exportService;
        _localizer = localizer;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public SelectList RoomList { get; private set; } = default!;

    public async Task OnGetAsync()
    {
        RoomList = await RoomOptions.BuildAsync(_context);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Input.From is not null && Input.To is not null && Input.To < Input.From)
        {
            ModelState.AddModelError("Input.To", _localizer["EndDateBeforeStartDate"]);
        }

        if (!ModelState.IsValid)
        {
            RoomList = await RoomOptions.BuildAsync(_context, Input.RoomId);
            return Page();
        }

        var filter = new ReservationFilter(Input.RoomId, Input.OnlyBooked ? SlotStatus.Booked : null, Input.From, Input.To);
        var file = await _exportService.ExportAsync(filter, Input.Format);
        return File(file.Content, file.ContentType, file.FileName);
    }

    public sealed class InputModel
    {
        [Display(Name = "Room")]
        public int? RoomId { get; set; }

        [Display(Name = "From")]
        public DateOnly? From { get; set; }

        [Display(Name = "To")]
        public DateOnly? To { get; set; }

        [Display(Name = "OnlyBooked")]
        public bool OnlyBooked { get; set; }

        [Display(Name = "Format")]
        public ExportFormat Format { get; set; } = ExportFormat.Xlsx;
    }
}
