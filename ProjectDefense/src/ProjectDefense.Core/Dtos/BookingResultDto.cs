namespace ProjectDefense.Core.Dtos;

public sealed record BookingResultDto(int SlotId, string StudentEmail, DateTime StartTime, DateTime EndTime, string Label);
