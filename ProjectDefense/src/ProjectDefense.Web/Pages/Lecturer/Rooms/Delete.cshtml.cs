using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using ProjectDefense.Core.Entities;
using ProjectDefense.Web.Data;

namespace ProjectDefense.Web.Pages.Lecturer.Rooms;

public class DeleteModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public DeleteModel(ApplicationDbContext context, IStringLocalizer<SharedResource> localizer)
    {
        _context = context;
        _localizer = localizer;
    }

    public Room Room { get; private set; } = new();

    public bool HasAvailabilities { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var room = await _context.Rooms.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
        if (room is null)
        {
            return NotFound();
        }

        Room = room;
        HasAvailabilities = await _context.LecturerAvailabilities.AnyAsync(a => a.RoomId == id);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var room = await _context.Rooms.FindAsync(id);
        if (room is null)
        {
            return NotFound();
        }

        if (await _context.LecturerAvailabilities.AnyAsync(a => a.RoomId == id))
        {
            TempData["Error"] = _localizer["RoomHasAvailabilities"].Value;
            return RedirectToPage("Delete", new { id });
        }

        _context.Rooms.Remove(room);
        await _context.SaveChangesAsync();
        TempData["Success"] = _localizer["RoomDeleted"].Value;
        return RedirectToPage("Index");
    }
}
