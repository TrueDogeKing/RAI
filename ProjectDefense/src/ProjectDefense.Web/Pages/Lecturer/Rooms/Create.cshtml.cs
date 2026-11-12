using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using ProjectDefense.Core.Entities;
using ProjectDefense.Web.Data;

namespace ProjectDefense.Web.Pages.Lecturer.Rooms;

public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CreateModel(ApplicationDbContext context, IStringLocalizer<SharedResource> localizer)
    {
        _context = context;
        _localizer = localizer;
    }

    [BindProperty]
    public Room Room { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (await _context.Rooms.AnyAsync(r => r.RoomNumber == Room.RoomNumber))
        {
            ModelState.AddModelError("Room.RoomNumber", _localizer["RoomNumberTaken"]);
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        Room.Id = 0;
        _context.Rooms.Add(Room);
        await _context.SaveChangesAsync();
        TempData["Success"] = _localizer["RoomSaved"].Value;
        return RedirectToPage("Index");
    }
}
