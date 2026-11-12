using System.ComponentModel.DataAnnotations;

namespace ProjectDefense.Core.Entities;

public class ReservationSlot
{
    public int Id { get; set; }

    public int LecturerAvailabilityId { get; set; }

    public LecturerAvailability? LecturerAvailability { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    [ConcurrencyCheck]
    public bool IsBlockedByLecturer { get; set; }

    [ConcurrencyCheck]
    public string? StudentId { get; set; }

    public ApplicationUser? Student { get; set; }
}
