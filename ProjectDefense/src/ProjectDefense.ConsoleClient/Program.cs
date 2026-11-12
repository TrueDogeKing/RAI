using System.Globalization;
using System.Net;
using System.Text;
using ProjectDefense.ConsoleClient;

Console.OutputEncoding = Encoding.UTF8;
CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("pl-PL");

var baseUrl = args.FirstOrDefault()
    ?? Environment.GetEnvironmentVariable("PROJECTDEFENSE_API")
    ?? "http://localhost:5080/";

using var http = new HttpClient { BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/") };
http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("pl-PL");
var client = new ApiClient(http);

Console.WriteLine($"ProjectDefense - klient konsolowy ({http.BaseAddress})");

while (true)
{
    Console.WriteLine();
    Console.WriteLine("1) Lista sal");
    Console.WriteLine("2) Wolne terminy");
    Console.WriteLine("3) Rezerwacja terminu");
    Console.WriteLine("0) Wyjscie");
    Console.Write("> ");

    var choice = Console.ReadLine()?.Trim();
    try
    {
        switch (choice)
        {
            case "1":
                await ShowRoomsAsync(client);
                break;
            case "2":
                await ShowSlotsAsync(client);
                break;
            case "3":
                await BookAsync(client);
                break;
            case "0":
            case null:
                return;
            default:
                Console.WriteLine("Nieznana opcja.");
                break;
        }
    }
    catch (HttpRequestException exception)
    {
        WriteColored($"Brak polaczenia z API: {exception.Message}", ConsoleColor.Red);
    }
}

static async Task ShowRoomsAsync(ApiClient client)
{
    var rooms = await client.GetRoomsAsync();
    ConsoleTable.Write(
        ["Id", "Numer", "Nazwa", "Stanowiska"],
        rooms.Select(r => new[] { r.Id.ToString(), r.RoomNumber, r.Name, r.WorkstationsCount.ToString() }));
}

static async Task ShowSlotsAsync(ApiClient client)
{
    Console.Write("Id sali (Enter = wszystkie): ");
    var input = Console.ReadLine();
    int? roomId = int.TryParse(input, out var parsed) ? parsed : null;

    var slots = await client.GetAvailableSlotsAsync(roomId);
    if (slots.Count == 0)
    {
        Console.WriteLine("Brak wolnych terminow.");
        return;
    }

    ConsoleTable.Write(
        ["Id", "Sala", "Data", "Godziny", "Prowadzacy"],
        slots.Select(s => new[]
        {
            s.Id.ToString(),
            s.RoomNumber,
            s.StartTime.ToString("ddd dd.MM.yyyy"),
            $"{s.StartTime:HH:mm}-{s.EndTime:HH:mm}",
            s.LecturerName
        }));
}

static async Task BookAsync(ApiClient client)
{
    Console.Write("Id lub e-mail studenta: ");
    var student = Console.ReadLine()?.Trim() ?? string.Empty;
    Console.Write("Id terminu: ");
    if (!int.TryParse(Console.ReadLine(), out var slotId))
    {
        Console.WriteLine("Niepoprawny identyfikator terminu.");
        return;
    }

    var outcome = await client.BookAsync(slotId, student);
    if (outcome.StatusCode == HttpStatusCode.OK && outcome.Booking is not null)
    {
        WriteColored($"200 OK - zarezerwowano: {outcome.Booking.Label} dla {outcome.Booking.StudentEmail}", ConsoleColor.Green);
    }
    else
    {
        WriteColored($"{(int)outcome.StatusCode} {outcome.StatusCode} - {outcome.Error}", ConsoleColor.Yellow);
    }
}

static void WriteColored(string message, ConsoleColor color)
{
    var previous = Console.ForegroundColor;
    Console.ForegroundColor = color;
    Console.WriteLine(message);
    Console.ForegroundColor = previous;
}
