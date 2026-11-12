namespace ProjectDefense.Web.Services;

public static class AvailabilityValidator
{
    public const int MinSlotDuration = 5;
    public const int MaxSlotDuration = 180;

    public static IReadOnlyList<string> Validate(AvailabilityRequest request, DateOnly today)
    {
        var errors = new List<string>();

        if (request.SlotDurationMinutes is < MinSlotDuration or > MaxSlotDuration)
        {
            errors.Add("SlotDurationOutOfRange");
        }

        if (request.EndDate < request.StartDate)
        {
            errors.Add("EndDateBeforeStartDate");
        }

        if (request.StartDate < today)
        {
            errors.Add("StartDateInPast");
        }

        var dailyMinutes = (request.DailyEndTime - request.DailyStartTime).TotalMinutes;
        if (request.DailyEndTime <= request.DailyStartTime || dailyMinutes < request.SlotDurationMinutes)
        {
            errors.Add("DailyRangeTooShort");
        }

        return errors;
    }

    public static bool Overlaps<T>(T startA, T endA, T startB, T endB)
        where T : IComparable<T>
        => startA.CompareTo(endB) < 0 && endA.CompareTo(startB) > 0;
}
