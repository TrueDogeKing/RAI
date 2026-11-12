using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using ProjectDefense.Core.Entities;
using ProjectDefense.Web.Data;

namespace ProjectDefense.Web.Pages.Lecturer.Rooms;

public class EditModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public EditModel(ApplicationDbContext context, IStringLocalizer<SharedResource> localizer)
    {
        _context = context;
        _localizer = localizer;
    }

    [BindProperty]
    public Room Room { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var room = await _context.Rooms.FindAsync(id);
        if (room is null)
        {
            return NotFound();
        }

        Room = room;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (await _context.Rooms.AnyAsync(r => r.RoomNumber == Room.RoomNumber && r.Id != id))
        {
            ModelState.AddModelError("Room.RoomNumber", _localizer["RoomNumberTaken"]);
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var room = await _context.Rooms.FindAsync(id);
        if (room is null)
        {
            return NotFound();
        }

        room.Name = Room.Name;
        room.RoomNumber = Room.RoomNumber;
        room.WorkstationsCount = Room.WorkstationsCount;
        await _context.SaveChangesAsync();
        TempData["Success"] = _localizer["RoomSaved"].Value;
        return RedirectToPage("Index");
    }
}
