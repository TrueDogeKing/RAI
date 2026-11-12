using ProjectDefense.Core.Entities;
using ProjectDefense.Web.Services;

namespace ProjectDefense.Tests;

public class SlotGenerationTests
{
    private readonly SlotGenerator _generator = new();

    private static LecturerAvailability Window(TimeOnly start, TimeOnly end, int minutes, int days) => new()
    {
        StartDate = new DateOnly(2026, 11, 16),
        EndDate = new DateOnly(2026, 11, 16).AddDays(days - 1),
        DailyStartTime = start,
        DailyEndTime = end,
        SlotDurationMinutes = minutes
    };

    [Fact]
    public void Two_days_from_8_to_10_with_15_minute_slots_produce_16_slots()
    {
        var slots = _generator.Generate(Window(new TimeOnly(8, 0), new TimeOnly(10, 0), 15, 2));

        slots.Should().HaveCount(16);
    }

    [Fact]
    public void Generated_slots_do_not_overlap_and_have_requested_length()
    {
        var slots = _generator.Generate(Window(new TimeOnly(8, 0), new TimeOnly(10, 0), 15, 2))
            .OrderBy(s => s.StartTime)
            .ToList();

        slots.Should().OnlyContain(s => s.EndTime - s.StartTime == TimeSpan.FromMinutes(15));
        slots.Zip(slots.Skip(1)).Should().OnlyContain(pair => pair.First.EndTime <= pair.Second.StartTime);
        slots.Select(s => s.StartTime).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Slots_stay_within_daily_hours_of_each_day()
    {
        var slots = _generator.Generate(Window(new TimeOnly(8, 0), new TimeOnly(10, 0), 15, 2));

        slots.Should().OnlyContain(s => s.StartTime.TimeOfDay >= new TimeSpan(8, 0, 0) && s.EndTime.TimeOfDay <= new TimeSpan(10, 0, 0));
        slots.GroupBy(s => s.StartTime.Date).Should().HaveCount(2).And.OnlyContain(day => day.Count() == 8);
        slots.Min(s => s.StartTime).Should().Be(new DateTime(2026, 11, 16, 8, 0, 0));
        slots.Max(s => s.EndTime).Should().Be(new DateTime(2026, 11, 17, 10, 0, 0));
    }

    [Fact]
    public void Incomplete_trailing_slot_is_not_generated()
    {
        var slots = _generator.Generate(Window(new TimeOnly(8, 0), new TimeOnly(9, 0), 25, 1));

        slots.Should().HaveCount(2);
        slots.Last().EndTime.Should().Be(new DateTime(2026, 11, 16, 8, 50, 0));
    }

    [Theory]
    [InlineData(4, true)]
    [InlineData(5, false)]
    [InlineData(180, false)]
    [InlineData(181, true)]
    public void Slot_duration_must_be_between_5_and_180_minutes(int minutes, bool expectError)
    {
        var request = new AvailabilityRequest(1, new DateOnly(2026, 11, 17), new DateOnly(2026, 11, 17), new TimeOnly(8, 0), new TimeOnly(11, 0), minutes);

        var errors = AvailabilityValidator.Validate(request, new DateOnly(2026, 11, 16));

        errors.Contains("SlotDurationOutOfRange").Should().Be(expectError);
    }

    [Fact]
    public void End_date_before_start_date_and_too_short_daily_range_are_rejected()
    {
        var request = new AvailabilityRequest(1, new DateOnly(2026, 11, 20), new DateOnly(2026, 11, 19), new TimeOnly(10, 0), new TimeOnly(10, 10), 15);

        var errors = AvailabilityValidator.Validate(request, new DateOnly(2026, 11, 16));

        errors.Should().Contain(new[] { "EndDateBeforeStartDate", "DailyRangeTooShort" });
    }
}
