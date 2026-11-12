using System.Globalization;
using Microsoft.Extensions.Localization;
using ProjectDefense.Core.Entities;

namespace ProjectDefense.Web.Services;

public interface ISlotLabelFormatter
{
    string Format(string roomNumber, string roomName, DateTime start, DateTime end);

    string Format(ReservationSlot slot);
}

public sealed class SlotLabelFormatter : ISlotLabelFormatter
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public SlotLabelFormatter(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;
    }

    public string Format(string roomNumber, string roomName, DateTime start, DateTime end)
    {
        var culture = CultureInfo.CurrentCulture;
        var day = start.ToString("ddd", culture).TrimEnd('.');
        day = culture.TextInfo.ToTitleCase(day) + ".";
        var date = start.ToString(_localizer["SlotDateFormat"], culture);
        return _localizer[
            "SlotLabel",
            roomNumber,
            roomName,
            $"{day} {date}",
            start.ToString("HH:mm", culture),
            end.ToString("HH:mm", culture)];
    }

    public string Format(ReservationSlot slot)
    {
        var room = slot.LecturerAvailability?.Room
            ?? throw new InvalidOperationException("Slot must be loaded together with its room.");
        return Format(room.RoomNumber, room.Name, slot.StartTime, slot.EndTime);
    }
}
