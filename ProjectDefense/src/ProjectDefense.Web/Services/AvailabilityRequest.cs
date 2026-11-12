namespace ProjectDefense.Web.Services;

public sealed record AvailabilityRequest(
    int RoomId,
    DateOnly StartDate,
    DateOnly EndDate,
    TimeOnly DailyStartTime,
    TimeOnly DailyEndTime,
    int SlotDurationMinutes);
