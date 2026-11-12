using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ProjectDefense.Core.Common;
using ProjectDefense.Core.Dtos;
using ProjectDefense.Web.Data;
using ProjectDefense.Web.Services;

namespace ProjectDefense.Web.Api;

public static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapProjectDefenseApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/rooms", GetRoomsAsync)
            .WithName("GetRooms")
            .WithTags("Rooms")
            .WithSummary("Lista sal");

        api.MapGet("/slots/available", GetAvailableSlotsAsync)
            .WithName("GetAvailableSlots")
            .WithTags("Slots")
            .WithSummary("Wolne, niezablokowane i przyszle terminy");

        api.MapPost("/slots/{id:int}/book", BookSlotAsync)
            .WithName("BookSlot")
            .WithTags("Slots")
            .WithSummary("Rezerwacja terminu dla studenta wskazanego identyfikatorem lub adresem e-mail");

        return app;
    }

    private static async Task<Ok<List<RoomDto>>> GetRoomsAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var rooms = await context.Rooms
            .AsNoTracking()
            .OrderBy(r => r.RoomNumber)
            .Select(r => new RoomDto(r.Id, r.Name, r.RoomNumber, r.WorkstationsCount))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(rooms);
    }

    private static async Task<Ok<List<AvailableSlotDto>>> GetAvailableSlotsAsync(
        int? roomId,
        IReservationService reservationService,
        ISlotLabelFormatter labelFormatter,
        CancellationToken cancellationToken)
    {
        var slots = await reservationService.GetAvailableSlotsAsync(roomId, cancellationToken);
        var result = slots
            .Select(s => new AvailableSlotDto(
                s.Id,
                s.LecturerAvailability!.RoomId,
                s.LecturerAvailability.Room!.RoomNumber,
                s.LecturerAvailability.Room.Name,
                s.LecturerAvailability.Lecturer?.FullName ?? string.Empty,
                s.StartTime,
                s.EndTime,
                labelFormatter.Format(s)))
            .ToList();

        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<BookingResultDto>, ValidationProblem, ProblemHttpResult>> BookSlotAsync(
        int id,
        BookSlotRequestDto request,
        ApplicationDbContext context,
        IReservationService reservationService,
        ISlotLabelFormatter labelFormatter,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Student))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(BookSlotRequestDto.Student)] = ["Student identifier or e-mail is required."]
            });
        }

        var key = request.Student.Trim();
        var normalized = key.ToUpperInvariant();
        var student = await context.Users
            .AsNoTracking()
            .Where(u => u.Id == key || u.NormalizedEmail == normalized)
            .Select(u => new { u.Id, u.Email })
            .FirstOrDefaultAsync(cancellationToken);

        if (student is null)
        {
            return Problem(OperationError.NotFound, "Student not found.");
        }

        var studentRoleId = await context.Roles.Where(r => r.Name == Roles.Student).Select(r => r.Id).FirstOrDefaultAsync(cancellationToken);
        if (!await context.UserRoles.AnyAsync(ur => ur.UserId == student.Id && ur.RoleId == studentRoleId, cancellationToken))
        {
            return Problem(OperationError.Forbidden, "Only users in the Student role can book slots.");
        }

        var result = await reservationService.BookAsync(id, student.Id, cancellationToken);
        if (!result.Succeeded)
        {
            return Problem(result.Error, result.Message ?? "Booking failed.");
        }

        var slot = result.Value!;
        return TypedResults.Ok(new BookingResultDto(slot.Id, student.Email ?? key, slot.StartTime, slot.EndTime, labelFormatter.Format(slot)));
    }

    private static ProblemHttpResult Problem(OperationError error, string detail)
    {
        var status = error switch
        {
            OperationError.NotFound => StatusCodes.Status404NotFound,
            OperationError.Conflict => StatusCodes.Status409Conflict,
            OperationError.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest
        };

        return TypedResults.Problem(detail: detail, statusCode: status, title: error.ToString());
    }
}
