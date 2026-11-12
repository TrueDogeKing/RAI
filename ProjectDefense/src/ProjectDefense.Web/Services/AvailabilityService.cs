using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using ProjectDefense.Core.Common;
using ProjectDefense.Core.Entities;
using ProjectDefense.Web.Data;

namespace ProjectDefense.Web.Services;

public interface IAvailabilityService
{
    Task<OperationResult<LecturerAvailability>> CreateAsync(string lecturerId, AvailabilityRequest request, CancellationToken cancellationToken = default);

    Task<bool> HasConflictAsync(AvailabilityRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AvailabilitySummary>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<OperationResult> DeleteAsync(int availabilityId, CancellationToken cancellationToken = default);
}

public sealed record AvailabilitySummary(
    int Id,
    string RoomNumber,
    string RoomName,
    string LecturerName,
    DateOnly StartDate,
    DateOnly EndDate,
    TimeOnly DailyStartTime,
    TimeOnly DailyEndTime,
    int SlotDurationMinutes,
    int SlotCount,
    int BookedCount);

public sealed class AvailabilityService : IAvailabilityService
{
    private readonly ApplicationDbContext _context;
    private readonly ISlotGenerator _slotGenerator;
    private readonly TimeProvider _timeProvider;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public AvailabilityService(
        ApplicationDbContext context,
        ISlotGenerator slotGenerator,
        TimeProvider timeProvider,
        IStringLocalizer<SharedResource> localizer)
    {
        _context = context;
        _slotGenerator = slotGenerator;
        _timeProvider = timeProvider;
        _localizer = localizer;
    }

    private DateTime Now => _timeProvider.GetLocalNow().DateTime;

    public async Task<OperationResult<LecturerAvailability>> CreateAsync(
        string lecturerId,
        AvailabilityRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = AvailabilityValidator.Validate(request, DateOnly.FromDateTime(Now));
        if (errors.Count > 0)
        {
            var message = string.Join(" ", errors.Select(key => _localizer[key, AvailabilityValidator.MinSlotDuration, AvailabilityValidator.MaxSlotDuration].Value));
            return OperationResult<LecturerAvailability>.Fail(OperationError.Validation, message);
        }

        if (!await _context.Rooms.AnyAsync(r => r.Id == request.RoomId, cancellationToken))
        {
            return OperationResult<LecturerAvailability>.Fail(OperationError.NotFound, _localizer["RoomNotFound"]);
        }

        if (await HasConflictAsync(request, cancellationToken))
        {
            return OperationResult<LecturerAvailability>.Fail(OperationError.Conflict, _localizer["RoomConflict"]);
        }

        var availability = new LecturerAvailability
        {
            LecturerId = lecturerId,
            RoomId = request.RoomId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            DailyStartTime = request.DailyStartTime,
            DailyEndTime = request.DailyEndTime,
            SlotDurationMinutes = request.SlotDurationMinutes
        };

        var slots = _slotGenerator.Generate(availability)
            .Where(slot => slot.StartTime > Now)
            .ToList();

        if (slots.Count == 0)
        {
            return OperationResult<LecturerAvailability>.Fail(OperationError.Validation, _localizer["NoSlotsGenerated"]);
        }

        availability.Slots = slots;
        _context.LecturerAvailabilities.Add(availability);
        await _context.SaveChangesAsync(cancellationToken);

        return OperationResult<LecturerAvailability>.Success(availability);
    }

    public Task<bool> HasConflictAsync(AvailabilityRequest request, CancellationToken cancellationToken = default)
        => _context.LecturerAvailabilities.AnyAsync(
            a => a.RoomId == request.RoomId
                && a.StartDate <= request.EndDate
                && a.EndDate >= request.StartDate
                && a.DailyStartTime < request.DailyEndTime
                && a.DailyEndTime > request.DailyStartTime,
            cancellationToken);

    public async Task<IReadOnlyList<AvailabilitySummary>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.LecturerAvailabilities
            .AsNoTracking()
            .OrderBy(a => a.StartDate)
            .ThenBy(a => a.DailyStartTime)
            .Select(a => new AvailabilitySummary(
                a.Id,
                a.Room!.RoomNumber,
                a.Room.Name,
                a.Lecturer!.FullName,
                a.StartDate,
                a.EndDate,
                a.DailyStartTime,
                a.DailyEndTime,
                a.SlotDurationMinutes,
                a.Slots.Count,
                a.Slots.Count(s => s.StudentId != null)))
            .ToListAsync(cancellationToken);

    public async Task<OperationResult> DeleteAsync(int availabilityId, CancellationToken cancellationToken = default)
    {
        var availability = await _context.LecturerAvailabilities
            .Include(a => a.Slots)
            .FirstOrDefaultAsync(a => a.Id == availabilityId, cancellationToken);

        if (availability is null)
        {
            return OperationResult.Fail(OperationError.NotFound, _localizer["AvailabilityNotFound"]);
        }

        if (availability.Slots.Any(s => s.StartTime <= Now))
        {
            return OperationResult.Fail(OperationError.Validation, _localizer["AvailabilityHasPastSlots"]);
        }

        if (availability.Slots.Any(s => s.StudentId != null))
        {
            return OperationResult.Fail(OperationError.Conflict, _localizer["AvailabilityHasBookings"]);
        }

        _context.LecturerAvailabilities.Remove(availability);
        await _context.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}
