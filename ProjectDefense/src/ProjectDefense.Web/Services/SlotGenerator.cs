using ProjectDefense.Core.Entities;

namespace ProjectDefense.Web.Services;

public interface ISlotGenerator
{
    IReadOnlyList<ReservationSlot> Generate(LecturerAvailability availability);
}

public sealed class SlotGenerator : ISlotGenerator
{
    public IReadOnlyList<ReservationSlot> Generate(LecturerAvailability availability)
    {
        if (availability.SlotDurationMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(availability), "Slot duration must be positive.");
        }

        var duration = TimeSpan.FromMinutes(availability.SlotDurationMinutes);
        var slots = new List<ReservationSlot>();

        for (var day = availability.StartDate; day <= availability.EndDate; day = day.AddDays(1))
        {
            var start = day.ToDateTime(availability.DailyStartTime);
            var dayEnd = day.ToDateTime(availability.DailyEndTime);

            while (start + duration <= dayEnd)
            {
                slots.Add(new ReservationSlot
                {
                    LecturerAvailability = availability,
                    StartTime = start,
                    EndTime = start + duration
                });
                start += duration;
            }
        }

        return slots;
    }
}
