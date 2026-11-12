using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using ProjectDefense.Core.Common;
using ProjectDefense.Core.Entities;
using ProjectDefense.Web.Data;

namespace ProjectDefense.Web.Services;

public sealed record StudentSummary(
    string Id,
    string FullName,
    string Email,
    string? IndexNumber,
    bool IsBanned,
    string? BanReason,
    DateTime? BannedUntil,
    DateTime? ActiveReservationStart);

public interface IBanService
{
    Task<bool> IsBannedAsync(string studentId, CancellationToken cancellationToken = default);

    Task<OperationResult> BanAsync(string studentId, string lecturerId, string reason, DateTime? bannedUntil, CancellationToken cancellationToken = default);

    Task<OperationResult> UnbanAsync(string studentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudentSummary>> GetStudentsAsync(CancellationToken cancellationToken = default);
}

public sealed class BanService : IBanService
{
    private readonly ApplicationDbContext _context;
    private readonly TimeProvider _timeProvider;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public BanService(ApplicationDbContext context, TimeProvider timeProvider, IStringLocalizer<SharedResource> localizer)
    {
        _context = context;
        _timeProvider = timeProvider;
        _localizer = localizer;
    }

    private DateTime Now => _timeProvider.GetLocalNow().DateTime;

    public async Task<bool> IsBannedAsync(string studentId, CancellationToken cancellationToken = default)
    {
        var flagged = await _context.Users
            .Where(u => u.Id == studentId)
            .Select(u => u.IsBanned)
            .FirstOrDefaultAsync(cancellationToken);

        if (!flagged)
        {
            return false;
        }

        var bans = await _context.StudentBans
            .Where(b => b.StudentId == studentId && b.LiftedAt == null)
            .Select(b => b.BannedUntil)
            .ToListAsync(cancellationToken);

        var now = Now;
        return bans.Count == 0 || bans.Any(until => until is null || until > now);
    }

    public async Task<OperationResult> BanAsync(
        string studentId,
        string lecturerId,
        string reason,
        DateTime? bannedUntil,
        CancellationToken cancellationToken = default)
    {
        var now = Now;
        if (bannedUntil is not null && bannedUntil <= now)
        {
            return OperationResult.Fail(OperationError.Validation, _localizer["BanUntilInPast"]);
        }

        var student = await FindStudentAsync(studentId, cancellationToken);
        if (student is null)
        {
            return OperationResult.Fail(OperationError.NotFound, _localizer["StudentNotFound"]);
        }

        student.IsBanned = true;
        _context.StudentBans.Add(new StudentBan
        {
            StudentId = studentId,
            BannedByLecturerId = lecturerId,
            Reason = reason.Trim(),
            BannedAt = now,
            BannedUntil = bannedUntil
        });

        var futureReservations = await _context.ReservationSlots
            .Where(s => s.StudentId == studentId && s.StartTime > now)
            .ToListAsync(cancellationToken);
        futureReservations.ForEach(s => s.StudentId = null);

        await _context.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    public async Task<OperationResult> UnbanAsync(string studentId, CancellationToken cancellationToken = default)
    {
        var student = await FindStudentAsync(studentId, cancellationToken);
        if (student is null)
        {
            return OperationResult.Fail(OperationError.NotFound, _localizer["StudentNotFound"]);
        }

        var now = Now;
        student.IsBanned = false;
        var activeBans = await _context.StudentBans
            .Where(b => b.StudentId == studentId && b.LiftedAt == null)
            .ToListAsync(cancellationToken);
        activeBans.ForEach(b => b.LiftedAt = now);

        await _context.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    public async Task<IReadOnlyList<StudentSummary>> GetStudentsAsync(CancellationToken cancellationToken = default)
    {
        var now = Now;
        var studentRoleId = await _context.Roles
            .Where(r => r.Name == Roles.Student)
            .Select(r => r.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var students = await _context.Users
            .AsNoTracking()
            .Where(u => _context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == studentRoleId))
            .OrderBy(u => u.FullName)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.StudentIndexNumber,
                u.IsBanned,
                Ban = _context.StudentBans
                    .Where(b => b.StudentId == u.Id && b.LiftedAt == null)
                    .OrderByDescending(b => b.BannedAt)
                    .Select(b => new { b.Reason, b.BannedUntil })
                    .FirstOrDefault(),
                Active = _context.ReservationSlots
                    .Where(s => s.StudentId == u.Id && s.StartTime > now)
                    .OrderBy(s => s.StartTime)
                    .Select(s => (DateTime?)s.StartTime)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return students
            .Select(s => new StudentSummary(
                s.Id,
                s.FullName,
                s.Email ?? string.Empty,
                s.StudentIndexNumber,
                s.IsBanned && (s.Ban is null || s.Ban.BannedUntil is null || s.Ban.BannedUntil > now),
                s.Ban?.Reason,
                s.Ban?.BannedUntil,
                s.Active))
            .ToList();
    }

    private async Task<ApplicationUser?> FindStudentAsync(string studentId, CancellationToken cancellationToken)
    {
        var studentRoleId = await _context.Roles
            .Where(r => r.Name == Roles.Student)
            .Select(r => r.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return await _context.Users
            .Where(u => u.Id == studentId && _context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == studentRoleId))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
