using System.ComponentModel.DataAnnotations;

namespace ProjectDefense.Core.Entities;

public class LecturerAvailability
{
    public int Id { get; set; }

    [Required]
    public string LecturerId { get; set; } = string.Empty;

    public ApplicationUser? Lecturer { get; set; }

    public int RoomId { get; set; }

    public Room? Room { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public TimeOnly DailyStartTime { get; set; }

    public TimeOnly DailyEndTime { get; set; }

    [Range(5, 180)]
    public int SlotDurationMinutes { get; set; }

    public ICollection<ReservationSlot> Slots { get; set; } = new List<ReservationSlot>();
}
