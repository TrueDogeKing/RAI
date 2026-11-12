using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ProjectDefense.Core.Dtos;

namespace ProjectDefense.ConsoleClient;

public sealed class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<RoomDto>> GetRoomsAsync()
        => await _http.GetFromJsonAsync<List<RoomDto>>("api/rooms") ?? [];

    public async Task<IReadOnlyList<AvailableSlotDto>> GetAvailableSlotsAsync(int? roomId)
    {
        var url = roomId is null ? "api/slots/available" : $"api/slots/available?roomId={roomId}";
        return await _http.GetFromJsonAsync<List<AvailableSlotDto>>(url) ?? [];
    }

    public async Task<BookingOutcome> BookAsync(int slotId, string student)
    {
        using var response = await _http.PostAsJsonAsync($"api/slots/{slotId}/book", new BookSlotRequestDto(student));

        if (response.IsSuccessStatusCode)
        {
            var booking = await response.Content.ReadFromJsonAsync<BookingResultDto>();
            return new BookingOutcome(response.StatusCode, booking, null);
        }

        var problem = await ReadProblemAsync(response);
        return new BookingOutcome(response.StatusCode, null, problem);
    }

    private static async Task<string> ReadProblemAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body))
        {
            return response.ReasonPhrase ?? response.StatusCode.ToString();
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                return string.Join("; ", errors.EnumerateObject()
                    .SelectMany(e => e.Value.EnumerateArray())
                    .Select(v => v.GetString()));
            }

            if (root.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
            {
                return detail.GetString()!;
            }

            return root.TryGetProperty("title", out var title) ? title.GetString() ?? body : body;
        }
        catch (JsonException)
        {
            return body;
        }
    }
}

public sealed record BookingOutcome(HttpStatusCode StatusCode, BookingResultDto? Booking, string? Error);
