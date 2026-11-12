namespace ProjectDefense.Core.Dtos;

public sealed record AvailableSlotDto(
    int Id,
    int RoomId,
    string RoomNumber,
    string RoomName,
    string LecturerName,
    DateTime StartTime,
    DateTime EndTime,
    string Label);
