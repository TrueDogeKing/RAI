using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ProjectDefense.Core.Entities;
using ProjectDefense.Web.Data;

namespace ProjectDefense.Web.Pages.Lecturer.Rooms;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public IList<Room> Rooms { get; private set; } = new List<Room>();

    public async Task OnGetAsync()
    {
        Rooms = await _context.Rooms.AsNoTracking().OrderBy(r => r.RoomNumber).ToListAsync();
    }
}
