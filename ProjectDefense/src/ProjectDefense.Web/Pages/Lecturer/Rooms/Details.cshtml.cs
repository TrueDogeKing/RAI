using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProjectDefense.Core.Entities;
using ProjectDefense.Web.Data;

namespace ProjectDefense.Web.Pages.Lecturer.Rooms;

public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly TimeProvider _timeProvider;

    public DetailsModel(ApplicationDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public Room Room { get; private set; } = new();

    public int AvailabilityCount { get; private set; }

    public int UpcomingBookings { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var room = await _context.Rooms.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
        if (room is null)
        {
            return NotFound();
        }

        var now = _timeProvider.GetLocalNow().DateTime;
        Room = room;
        AvailabilityCount = await _context.LecturerAvailabilities.CountAsync(a => a.RoomId == id);
        UpcomingBookings = await _context.ReservationSlots.CountAsync(s => s.LecturerAvailability!.RoomId == id && s.StudentId != null && s.StartTime > now);
        return Page();
    }
}
