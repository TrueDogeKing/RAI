using ProjectDefense.Core.Common;
using ProjectDefense.Tests.Support;

namespace ProjectDefense.Tests;

public class RoomConflictDetectionTests : IDisposable
{
    private readonly BookingTestContext _ctx = new();

    [Fact]
    public async Task Overlapping_window_in_the_same_room_is_rejected()
    {
        (await _ctx.Availability.CreateAsync(_ctx.Lecturer.Id, _ctx.Window(_ctx.RoomB214, 8, 10))).Succeeded.Should().BeTrue();

        var result = await _ctx.Availability.CreateAsync(_ctx.Lecturer.Id, _ctx.Window(_ctx.RoomB214, 9, 11));

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(OperationError.Conflict);
        result.Message.Should().Be("RoomConflict");
        _ctx.Db.LecturerAvailabilities.Should().HaveCount(1);
    }

    [Fact]
    public async Task Same_hours_in_a_different_room_are_accepted()
    {
        await _ctx.Availability.CreateAsync(_ctx.Lecturer.Id, _ctx.Window(_ctx.RoomB214, 8, 10));

        var result = await _ctx.Availability.CreateAsync(_ctx.Lecturer.Id, _ctx.Window(_ctx.RoomA105, 9, 11));

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Adjacent_windows_touching_at_the_boundary_do_not_conflict()
    {
        await _ctx.Availability.CreateAsync(_ctx.Lecturer.Id, _ctx.Window(_ctx.RoomB214, 8, 10));

        var result = await _ctx.Availability.CreateAsync(_ctx.Lecturer.Id, _ctx.Window(_ctx.RoomB214, 10, 12));

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Overlapping_hours_on_disjoint_dates_do_not_conflict()
    {
        await _ctx.Availability.CreateAsync(_ctx.Lecturer.Id, _ctx.Window(_ctx.RoomB214, 8, 10));
        var later = _ctx.Window(_ctx.RoomB214, 9, 11) with
        {
            StartDate = BookingTestContext.Tomorrow.AddDays(7),
            EndDate = BookingTestContext.Tomorrow.AddDays(7)
        };

        var result = await _ctx.Availability.CreateAsync(_ctx.Lecturer.Id, later);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Window_fully_containing_an_existing_one_is_rejected()
    {
        await _ctx.Availability.CreateAsync(_ctx.Lecturer.Id, _ctx.Window(_ctx.RoomB214, 9, 10));

        var conflict = await _ctx.Availability.HasConflictAsync(_ctx.Window(_ctx.RoomB214, 8, 12, days: 3));

        conflict.Should().BeTrue();
    }

    public void Dispose() => _ctx.Dispose();
}
