using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using ProjectDefense.Core.Common;
using ProjectDefense.Core.Dtos;
using ProjectDefense.Core.Entities;
using ProjectDefense.Web.Data;

namespace ProjectDefense.Web.Services;

public sealed record ReservationFilter(int? RoomId = null, SlotStatus? Status = null, DateOnly? From = null, DateOnly? To = null);

public sealed record BlockResult(int BlockedSlots, int ReleasedReservations);

public interface IReservationService
{
    Task<IReadOnlyList<ReservationSlot>> GetAvailableSlotsAsync(int? roomId = null, CancellationToken cancellationToken = default);

    Task<ReservationSlot?> GetActiveReservationAsync(string studentId, CancellationToken cancellationToken = default);

    Task<OperationResult<ReservationSlot>> BookAsync(int slotId, string studentId, CancellationToken cancellationToken = default);

    Task<OperationResult> CancelAsync(int slotId, string studentId, CancellationToken cancellationToken = default);

    Task<OperationResult<ReservationSlot>> RescheduleAsync(string studentId, int newSlotId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReservationDto>> GetReservationsAsync(ReservationFilter filter, CancellationToken cancellationToken = default);

    Task<OperationResult> CancelByLecturerAsync(int slotId, CancellationToken cancellationToken = default);

    Task<OperationResult> ReassignAsync(int slotId, int targetSlotId, CancellationToken cancellationToken = default);

    Task<OperationResult<BlockResult>> BlockAsync(int roomId, DateTime from, DateTime to, CancellationToken cancellationToken = default);

    Task<OperationResult> UnblockAsync(int slotId, CancellationToken cancellationToken = default);

    Task<OperationResult> UpdateSlotAsync(int slotId, DateTime newStart, DateTime newEnd, CancellationToken cancellationToken = default);
}

public sealed class ReservationService : IReservationService
{
    private readonly ApplicationDbContext _context;
    private readonly IBanService _banService;
    private readonly TimeProvider _timeProvider;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ReservationService(
        ApplicationDbContext context,
        IBanService banService,
        TimeProvider timeProvider,
        IStringLocalizer<SharedResource> localizer)
    {
        _context = context;
        _banService = banService;
        _timeProvider = timeProvider;
        _localizer = localizer;
    }

    private DateTime Now => _timeProvider.GetLocalNow().DateTime;

    private IQueryable<ReservationSlot> SlotsWithRoom
        => _context.ReservationSlots
            .Include(s => s.LecturerAvailability!)
                .ThenInclude(a => a.Room)
            .Include(s => s.LecturerAvailability!)
                .ThenInclude(a => a.Lecturer);

    public async Task<IReadOnlyList<ReservationSlot>> GetAvailableSlotsAsync(int? roomId = null, CancellationToken cancellationToken = default)
    {
        var now = Now;
        var query = SlotsWithRoom
            .AsNoTracking()
            .Where(s => s.StudentId == null && !s.IsBlockedByLecturer && s.StartTime > now);

        if (roomId is not null)
        {
            query = query.Where(s => s.LecturerAvailability!.RoomId == roomId);
        }

        return await query.OrderBy(s => s.StartTime).ToListAsync(cancellationToken);
    }

    public Task<ReservationSlot?> GetActiveReservationAsync(string studentId, CancellationToken cancellationToken = default)
    {
        var now = Now;
        return SlotsWithRoom
            .AsNoTracking()
            .Where(s => s.StudentId == studentId && s.StartTime > now)
            .OrderBy(s => s.StartTime)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<OperationResult<ReservationSlot>> BookAsync(int slotId, string studentId, CancellationToken cancellationToken = default)
    {
        var studentCheck = await CheckStudentCanBookAsync(studentId, cancellationToken);
        if (!studentCheck.Succeeded)
        {
            return OperationResult<ReservationSlot>.Fail(studentCheck.Error, studentCheck.Message!);
        }

        if (await HasActiveReservationAsync(studentId, cancellationToken))
        {
            return OperationResult<ReservationSlot>.Fail(OperationError.Conflict, _localizer["OneActiveReservation"]);
        }

        var slot = await SlotsWithRoom.FirstOrDefaultAsync(s => s.Id == slotId, cancellationToken);
        var slotCheck = CheckSlotIsBookable(slot);
        if (!slotCheck.Succeeded)
        {
            return OperationResult<ReservationSlot>.Fail(slotCheck.Error, slotCheck.Message!);
        }

        slot!.StudentId = studentId;
        return await SaveAsync(slot, cancellationToken);
    }

    public async Task<OperationResult> CancelAsync(int slotId, string studentId, CancellationToken cancellationToken = default)
    {
        var slot = await _context.ReservationSlots.FirstOrDefaultAsync(s => s.Id == slotId, cancellationToken);
        if (slot is null || slot.StudentId != studentId)
        {
            return OperationResult.Fail(OperationError.NotFound, _localizer["ReservationNotFound"]);
        }

        if (IsPast(slot))
        {
            return OperationResult.Fail(OperationError.Validation, _localizer["SlotInPast"]);
        }

        slot.StudentId = null;
        var result = await SaveAsync(slot, cancellationToken);
        return result.Succeeded ? OperationResult.Success() : OperationResult.Fail(result.Error, result.Message!);
    }

    public async Task<OperationResult<ReservationSlot>> RescheduleAsync(string studentId, int newSlotId, CancellationToken cancellationToken = default)
    {
        var studentCheck = await CheckStudentCanBookAsync(studentId, cancellationToken);
        if (!studentCheck.Succeeded)
        {
            return OperationResult<ReservationSlot>.Fail(studentCheck.Error, studentCheck.Message!);
        }

        var now = Now;
        var current = await _context.ReservationSlots
            .Where(s => s.StudentId == studentId && s.StartTime > now)
            .OrderBy(s => s.StartTime)
            .FirstOrDefaultAsync(cancellationToken);

        if (current is null)
        {
            return OperationResult<ReservationSlot>.Fail(OperationError.NotFound, _localizer["ReservationNotFound"]);
        }

        var target = await SlotsWithRoom.FirstOrDefaultAsync(s => s.Id == newSlotId, cancellationToken);
        var slotCheck = CheckSlotIsBookable(target);
        if (!slotCheck.Succeeded)
        {
            return OperationResult<ReservationSlot>.Fail(slotCheck.Error, slotCheck.Message!);
        }

        current.StudentId = null;
        target!.StudentId = studentId;
        return await SaveAsync(target, cancellationToken);
    }

    public async Task<IReadOnlyList<ReservationDto>> GetReservationsAsync(ReservationFilter filter, CancellationToken cancellationToken = default)
    {
        var now = Now;
        var query = _context.ReservationSlots.AsNoTracking().AsQueryable();

        if (filter.RoomId is not null)
        {
            query = query.Where(s => s.LecturerAvailability!.RoomId == filter.RoomId);
        }

        if (filter.From is not null)
        {
            var from = filter.From.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(s => s.StartTime >= from);
        }

        if (filter.To is not null)
        {
            var to = filter.To.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            query = query.Where(s => s.StartTime < to);
        }

        query = filter.Status switch
        {
            SlotStatus.Past => query.Where(s => s.StartTime <= now),
            SlotStatus.Blocked => query.Where(s => s.StartTime > now && s.IsBlockedByLecturer),
            SlotStatus.Booked => query.Where(s => s.StartTime > now && !s.IsBlockedByLecturer && s.StudentId != null),
            SlotStatus.Free => query.Where(s => s.StartTime > now && !s.IsBlockedByLecturer && s.StudentId == null),
            _ => query
        };

        return await query
            .OrderBy(s => s.StartTime)
            .Select(s => new ReservationDto(
                s.Id,
                s.LecturerAvailability!.RoomId,
                s.LecturerAvailability.Room!.RoomNumber,
                s.LecturerAvailability.Room.Name,
                s.StartTime,
                s.EndTime,
                s.StartTime <= now
                    ? SlotStatus.Past
                    : s.IsBlockedByLecturer
                        ? SlotStatus.Blocked
                        : s.StudentId != null ? SlotStatus.Booked : SlotStatus.Free,
                s.StudentId,
                s.Student != null ? s.Student.FullName : null,
                s.Student != null ? s.Student.Email : null,
                s.Student != null ? s.Student.StudentIndexNumber : null))
            .ToListAsync(cancellationToken);
    }

    public async Task<OperationResult> CancelByLecturerAsync(int slotId, CancellationToken cancellationToken = default)
    {
        var slot = await _context.ReservationSlots.FirstOrDefaultAsync(s => s.Id == slotId, cancellationToken);
        if (slot?.StudentId is null)
        {
            return OperationResult.Fail(OperationError.NotFound, _localizer["ReservationNotFound"]);
        }

        if (IsPast(slot))
        {
            return OperationResult.Fail(OperationError.Validation, _localizer["SlotInPast"]);
        }

        slot.StudentId = null;
        var result = await SaveAsync(slot, cancellationToken);
        return result.Succeeded ? OperationResult.Success() : OperationResult.Fail(result.Error, result.Message!);
    }

    public async Task<OperationResult> ReassignAsync(int slotId, int targetSlotId, CancellationToken cancellationToken = default)
    {
        if (slotId == targetSlotId)
        {
            return OperationResult.Fail(OperationError.Validation, _localizer["SameSlot"]);
        }

        var source = await _context.ReservationSlots.FirstOrDefaultAsync(s => s.Id == slotId, cancellationToken);
        if (source?.StudentId is null)
        {
            return OperationResult.Fail(OperationError.NotFound, _localizer["ReservationNotFound"]);
        }

        if (IsPast(source))
        {
            return OperationResult.Fail(OperationError.Validation, _localizer["SlotInPast"]);
        }

        var target = await SlotsWithRoom.FirstOrDefaultAsync(s => s.Id == targetSlotId, cancellationToken);
        var slotCheck = CheckSlotIsBookable(target);
        if (!slotCheck.Succeeded)
        {
            return slotCheck;
        }

        target!.StudentId = source.StudentId;
        source.StudentId = null;
        var result = await SaveAsync(target, cancellationToken);
        return result.Succeeded ? OperationResult.Success() : OperationResult.Fail(result.Error, result.Message!);
    }

    public async Task<OperationResult<BlockResult>> BlockAsync(int roomId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        if (to <= from)
        {
            return OperationResult<BlockResult>.Fail(OperationError.Validation, _localizer["BlockRangeInvalid"]);
        }

        var now = Now;
        if (to <= now)
        {
            return OperationResult<BlockResult>.Fail(OperationError.Validation, _localizer["SlotInPast"]);
        }

        var slots = await _context.ReservationSlots
            .Where(s => s.LecturerAvailability!.RoomId == roomId
                && s.StartTime > now
                && s.StartTime < to
                && s.EndTime > from)
            .ToListAsync(cancellationToken);

        var released = slots.Count(s => s.StudentId != null);
        slots.ForEach(s =>
        {
            s.IsBlockedByLecturer = true;
            s.StudentId = null;
        });

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return OperationResult<BlockResult>.Fail(OperationError.Conflict, _localizer["ConcurrencyConflict"]);
        }

        return OperationResult<BlockResult>.Success(new BlockResult(slots.Count, released));
    }

    public async Task<OperationResult> UnblockAsync(int slotId, CancellationToken cancellationToken = default)
    {
        var slot = await _context.ReservationSlots.FirstOrDefaultAsync(s => s.Id == slotId, cancellationToken);
        if (slot is null)
        {
            return OperationResult.Fail(OperationError.NotFound, _localizer["SlotNotFound"]);
        }

        if (IsPast(slot))
        {
            return OperationResult.Fail(OperationError.Validation, _localizer["SlotInPast"]);
        }

        slot.IsBlockedByLecturer = false;
        await _context.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    public async Task<OperationResult> UpdateSlotAsync(int slotId, DateTime newStart, DateTime newEnd, CancellationToken cancellationToken = default)
    {
        var slot = await _context.ReservationSlots.FirstOrDefaultAsync(s => s.Id == slotId, cancellationToken);
        if (slot is null)
        {
            return OperationResult.Fail(OperationError.NotFound, _localizer["SlotNotFound"]);
        }

        if (IsPast(slot) || newStart <= Now)
        {
            return OperationResult.Fail(OperationError.Validation, _localizer["SlotInPast"]);
        }

        if (newEnd <= newStart)
        {
            return OperationResult.Fail(OperationError.Validation, _localizer["BlockRangeInvalid"]);
        }

        slot.StartTime = newStart;
        slot.EndTime = newEnd;
        await _context.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    private bool IsPast(ReservationSlot slot) => slot.StartTime <= Now;

    private Task<bool> HasActiveReservationAsync(string studentId, CancellationToken cancellationToken)
    {
        var now = Now;
        return _context.ReservationSlots.AnyAsync(s => s.StudentId == studentId && s.StartTime > now, cancellationToken);
    }

    private async Task<OperationResult> CheckStudentCanBookAsync(string studentId, CancellationToken cancellationToken)
    {
        if (!await _context.Users.AnyAsync(u => u.Id == studentId, cancellationToken))
        {
            return OperationResult.Fail(OperationError.NotFound, _localizer["StudentNotFound"]);
        }

        if (await _banService.IsBannedAsync(studentId, cancellationToken))
        {
            return OperationResult.Fail(OperationError.Forbidden, _localizer["StudentBanned"]);
        }

        return OperationResult.Success();
    }

    private OperationResult CheckSlotIsBookable(ReservationSlot? slot)
    {
        if (slot is null)
        {
            return OperationResult.Fail(OperationError.NotFound, _localizer["SlotNotFound"]);
        }

        if (IsPast(slot))
        {
            return OperationResult.Fail(OperationError.Validation, _localizer["SlotInPast"]);
        }

        if (slot.IsBlockedByLecturer)
        {
            return OperationResult.Fail(OperationError.Conflict, _localizer["SlotBlocked"]);
        }

        if (slot.StudentId is not null)
        {
            return OperationResult.Fail(OperationError.Conflict, _localizer["SlotTaken"]);
        }

        return OperationResult.Success();
    }

    private async Task<OperationResult<ReservationSlot>> SaveAsync(ReservationSlot slot, CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return OperationResult<ReservationSlot>.Success(slot);
        }
        catch (DbUpdateConcurrencyException)
        {
            return OperationResult<ReservationSlot>.Fail(OperationError.Conflict, _localizer["ConcurrencyConflict"]);
        }
    }
}
