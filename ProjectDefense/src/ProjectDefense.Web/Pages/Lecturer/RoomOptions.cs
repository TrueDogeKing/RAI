using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProjectDefense.Web.Data;

namespace ProjectDefense.Web.Pages.Lecturer;

public static class RoomOptions
{
    public static async Task<SelectList> BuildAsync(ApplicationDbContext context, int? selected = null)
    {
        var rooms = await context.Rooms
            .AsNoTracking()
            .OrderBy(r => r.RoomNumber)
            .Select(r => new { r.Id, Label = r.RoomNumber + " - " + r.Name })
            .ToListAsync();

        return new SelectList(rooms, "Id", "Label", selected);
    }
}
