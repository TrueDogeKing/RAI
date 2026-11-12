using Microsoft.EntityFrameworkCore;
using ProjectDefense.Core.Common;
using ProjectDefense.Core.Entities;
using ProjectDefense.Tests.Support;

namespace ProjectDefense.Tests;

public class StudentBookingRulesTests : IDisposable
{
    private readonly BookingTestContext _ctx = new();

    [Fact]
    public async Task Student_with_active_reservation_cannot_book_second_slot()
    {
        var slots = await _ctx.CreateSlotsAsync(_ctx.RoomB214);
        (await _ctx.Reservations.BookAsync(slots[0].Id, _ctx.FirstStudent.Id)).Succeeded.Should().BeTrue();

        var second = await _ctx.Reservations.BookAsync(slots[1].Id, _ctx.FirstStudent.Id);

        second.Succeeded.Should().BeFalse();
        second.Error.Should().Be(OperationError.Conflict);
        second.Message.Should().Be("OneActiveReservation");
        (await _ctx.Db.ReservationSlots.CountAsync(s => s.StudentId == _ctx.FirstStudent.Id)).Should().Be(1);
    }

    [Fact]
    public async Task Slot_taken_by_another_student_cannot_be_booked()
    {
        var slots = await _ctx.CreateSlotsAsync(_ctx.RoomB214);
        await _ctx.Reservations.BookAsync(slots[0].Id, _ctx.FirstStudent.Id);

        var result = await _ctx.Reservations.BookAsync(slots[0].Id, _ctx.SecondStudent.Id);

        result.Error.Should().Be(OperationError.Conflict);
        result.Message.Should().Be("SlotTaken");
    }

    [Fact]
    public async Task Modifying_a_past_slot_fails_for_every_operation()
    {
        var slots = await _ctx.CreateSlotsAsync(_ctx.RoomB214);
        await _ctx.Reservations.BookAsync(slots[0].Id, _ctx.FirstStudent.Id);
        var free = slots[1];

        _ctx.Time.Advance(TimeSpan.FromDays(2));

        (await _ctx.Reservations.CancelAsync(slots[0].Id, _ctx.FirstStudent.Id)).Message.Should().Be("SlotInPast");
        (await _ctx.Reservations.CancelByLecturerAsync(slots[0].Id)).Message.Should().Be("SlotInPast");
        (await _ctx.Reservations.BookAsync(free.Id, _ctx.SecondStudent.Id)).Message.Should().Be("SlotInPast");
        (await _ctx.Reservations.UpdateSlotAsync(free.Id, free.StartTime.AddDays(10), free.EndTime.AddDays(10))).Message.Should().Be("SlotInPast");
        (await _ctx.Reservations.UnblockAsync(free.Id)).Message.Should().Be("SlotInPast");

        var reloaded = await _ctx.Db.ReservationSlots.AsNoTracking().SingleAsync(s => s.Id == slots[0].Id);
        reloaded.StudentId.Should().Be(_ctx.FirstStudent.Id);
    }

    [Fact]
    public async Task Past_reservation_does_not_count_as_active()
    {
        var slots = await _ctx.CreateSlotsAsync(_ctx.RoomB214);
        await _ctx.Reservations.BookAsync(slots[0].Id, _ctx.FirstStudent.Id);
        var later = await _ctx.Availability.CreateAsync(_ctx.Lecturer.Id, _ctx.Window(_ctx.RoomA105, 8, 10) with
        {
            StartDate = BookingTestContext.Tomorrow.AddDays(5),
            EndDate = BookingTestContext.Tomorrow.AddDays(5)
        });

        _ctx.Time.Advance(TimeSpan.FromDays(2));
        var result = await _ctx.Reservations.BookAsync(later.Value!.Slots.First().Id, _ctx.FirstStudent.Id);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Rescheduling_releases_old_slot_and_assigns_new_one()
    {
        var slots = await _ctx.CreateSlotsAsync(_ctx.RoomB214);
        await _ctx.Reservations.BookAsync(slots[0].Id, _ctx.FirstStudent.Id);

        var result = await _ctx.Reservations.RescheduleAsync(_ctx.FirstStudent.Id, slots[3].Id);

        result.Succeeded.Should().BeTrue();
        var state = await _ctx.Db.ReservationSlots.AsNoTracking().ToDictionaryAsync(s => s.Id, s => s.StudentId);
        state[slots[0].Id].Should().BeNull();
        state[slots[3].Id].Should().Be(_ctx.FirstStudent.Id);
    }

    [Fact]
    public async Task Failed_reschedule_keeps_the_original_reservation()
    {
        var slots = await _ctx.CreateSlotsAsync(_ctx.RoomB214);
        await _ctx.Reservations.BookAsync(slots[0].Id, _ctx.FirstStudent.Id);
        await _ctx.Reservations.BookAsync(slots[1].Id, _ctx.SecondStudent.Id);

        var result = await _ctx.Reservations.RescheduleAsync(_ctx.FirstStudent.Id, slots[1].Id);

        result.Succeeded.Should().BeFalse();
        var original = await _ctx.Db.ReservationSlots.AsNoTracking().SingleAsync(s => s.Id == slots[0].Id);
        original.StudentId.Should().Be(_ctx.FirstStudent.Id);
    }

    [Fact]
    public async Task Blocking_a_period_releases_students_and_prevents_new_bookings()
    {
        var slots = await _ctx.CreateSlotsAsync(_ctx.RoomB214);
        await _ctx.Reservations.BookAsync(slots[1].Id, _ctx.FirstStudent.Id);

        var block = await _ctx.Reservations.BlockAsync(_ctx.RoomB214.Id, slots[0].StartTime, slots[3].EndTime);

        block.Value.Should().Be(new Web.Services.BlockResult(4, 1));
        (await _ctx.Reservations.GetActiveReservationAsync(_ctx.FirstStudent.Id)).Should().BeNull();
        (await _ctx.Reservations.BookAsync(slots[2].Id, _ctx.SecondStudent.Id)).Message.Should().Be("SlotBlocked");
        (await _ctx.Reservations.GetAvailableSlotsAsync()).Should().HaveCount(slots.Count - 4);
    }

    [Fact]
    public async Task Banned_student_cannot_book_and_loses_future_reservation()
    {
        var slots = await _ctx.CreateSlotsAsync(_ctx.RoomB214);
        await _ctx.Reservations.BookAsync(slots[0].Id, _ctx.FirstStudent.Id);

        (await _ctx.Bans.BanAsync(_ctx.FirstStudent.Id, _ctx.Lecturer.Id, "Nieobecnosc", null)).Succeeded.Should().BeTrue();
        var result = await _ctx.Reservations.BookAsync(slots[2].Id, _ctx.FirstStudent.Id);

        result.Error.Should().Be(OperationError.Forbidden);
        (await _ctx.Reservations.GetActiveReservationAsync(_ctx.FirstStudent.Id)).Should().BeNull();
    }

    [Fact]
    public async Task Temporary_ban_expires_automatically()
    {
        var slots = await _ctx.CreateSlotsAsync(_ctx.RoomB214);
        await _ctx.Bans.BanAsync(_ctx.FirstStudent.Id, _ctx.Lecturer.Id, "Spoznienie", BookingTestContext.Now.AddHours(6));

        (await _ctx.Bans.IsBannedAsync(_ctx.FirstStudent.Id)).Should().BeTrue();
        _ctx.Time.Advance(TimeSpan.FromHours(7));

        (await _ctx.Bans.IsBannedAsync(_ctx.FirstStudent.Id)).Should().BeFalse();
        (await _ctx.Reservations.BookAsync(slots[0].Id, _ctx.FirstStudent.Id)).Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Available_slots_exclude_booked_blocked_and_past_ones()
    {
        var slots = await _ctx.CreateSlotsAsync(_ctx.RoomB214);
        await _ctx.Reservations.BookAsync(slots[0].Id, _ctx.FirstStudent.Id);
        await _ctx.Reservations.BlockAsync(_ctx.RoomB214.Id, slots[1].StartTime, slots[1].EndTime);
        _ctx.Db.ReservationSlots.Add(new ReservationSlot
        {
            LecturerAvailabilityId = slots[0].LecturerAvailabilityId,
            StartTime = BookingTestContext.Now.AddHours(-1),
            EndTime = BookingTestContext.Now.AddMinutes(-45)
        });
        await _ctx.Db.SaveChangesAsync();

        var available = await _ctx.Reservations.GetAvailableSlotsAsync();

        available.Select(s => s.Id).Should().BeEquivalentTo(slots.Skip(2).Select(s => s.Id));
    }

    public void Dispose() => _ctx.Dispose();
}
