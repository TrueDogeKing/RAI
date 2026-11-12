using ProjectDefense.Core.Common;

namespace ProjectDefense.Core.Dtos;

public sealed record ReservationDto(
    int SlotId,
    int RoomId,
    string RoomNumber,
    string RoomName,
    DateTime StartTime,
    DateTime EndTime,
    SlotStatus Status,
    string? StudentId,
    string? StudentName,
    string? StudentEmail,
    string? StudentIndexNumber);
